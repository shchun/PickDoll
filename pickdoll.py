"""Interactive PickDoll simulator with automatic pickup and delivery."""

from math import hypot, sin, cos
from pathlib import Path
from time import perf_counter

import glfw
import mujoco
import numpy as np

import ctypes
import sys
from uuid import uuid4


MODEL_PATH = Path(__file__).with_name("claw.xml")
AXES = ("x", "y", "z")
PRIZES = ("prize", "prize_left", "prize_right") + tuple(
    f"prize_{number}" for number in range(4, 8)
)
DROP_TARGET = (-0.128, -0.128)
FINGERS = ("finger_a", "finger_b", "finger_c")
PHASE_LABELS = {
    "aligning": "Centering over prize",
    "lowering": "Lowering",
    "gripping": "Gripping",
    "raising": "Lifting",
    "settling": "Holding",
    "delivering": "Moving to prize chute",
    "releasing": "Dropping into collection bay",
    "fault": "Motion timeout - press R to restart",
}
VIEWS = (
    ("play", "Front"),
    ("overhead", "Overhead"),
    ("left_side", "Left"),
    ("right_side", "Right"),
    ("rear", "Rear"),
)


def aim_direction(view, horizontal, vertical):
    """Map screen directions onto the machine's horizontal axes."""
    if view == "left_side":
        return vertical, -horizontal
    if view == "right_side":
        return -vertical, horizontal
    if view == "rear":
        return -horizontal, -vertical
    return horizontal, vertical


class Claw:
    def __init__(self):
        self.model = mujoco.MjModel.from_xml_path(str(MODEL_PATH))
        self.data = mujoco.MjData(self.model)
        self.led_ids = np.array([
            index for index in range(self.model.ngeom)
            if (self.model.geom(index).name or "").startswith("led_")
        ], dtype=int)
        self.led_offsets = np.arange(len(self.led_ids)) * 0.34
        self.rng = np.random.default_rng()
        self.reset()

    def reset(self):
        mujoco.mj_resetData(self.model, self.data)
        # Randomize safe slots with small offsets to keep bodies separated.
        slots = np.array(
            [(-0.105, 0.05), (-0.105, 0.125)]
            + [(x, y) for x in (0.0, 0.105)
               for y in (-0.105, -0.030, 0.045, 0.120)]
        )
        chosen = self.rng.choice(len(slots), len(PRIZES), replace=False)
        positions = slots[chosen] + self.rng.uniform(-0.003, 0.003, (len(PRIZES), 2))
        for name, position in zip(PRIZES, positions):
            joint = self.model.body(name).jntadr[0]
            address = self.model.jnt_qposadr[joint]
            self.data.qpos[address:address + 3] = [*position, 0.035]
        self.audio_events = []
        self.celebration_until = 0.0
        self.closed = False
        self.phase = "manual"
        self.grip_time = 0.0
        self.phase_time = 0.0
        self.collected = set()
        self.message = "Ready"
        self.pickup_target = None
        self.nudge_time = 0.0
        self.nudge_direction = (0, 0)
        self.descent_stall = 0.0
        self.previous_height = 0.0
        self.animate_leds()
        mujoco.mj_forward(self.model, self.data)

    def animate_leds(self):
        phase = self.data.time * 2.2 + self.led_offsets
        colors = np.column_stack((
            0.78 + 0.22 * np.sin(phase),
            0.43 + 0.23 * np.sin(phase + 2.1),
            0.83 + 0.17 * np.sin(phase + 4.2),
        ))
        brightness = 0.64 + 0.36 * np.sin(phase * 0.8) ** 2
        if self.data.time < self.celebration_until:
            pulse = 0.75 + 0.25 * sin(self.data.time * 10) ** 2
            colors[:] = [1.0, 0.78, 0.90]
            brightness[:] = pulse
        self.model.geom_rgba[self.led_ids, :3] = colors * brightness[:, None]

    def start_pickup(self):
        if self.phase == "manual" and len(self.collected) < len(PRIZES):
            self.closed = False
            candidates = [
                name for name in PRIZES if name not in self.collected
                and self.data.body(name).xpos[2] > 0
            ]
            cx, cy = self.data.body("claw").xpos[:2]
            self.pickup_target = min(
                candidates,
                key=lambda name: hypot(
                    self.data.body(name).xpos[0] - cx,
                    self.data.body(name).xpos[1] - cy,
                ),
                default=None,
            )
            if self.pickup_target is not None:
                px, py = self.data.body(self.pickup_target).xpos[:2]
                if hypot(px - cx, py - cy) > 0.025:
                    self.pickup_target = None
            self.phase = "aligning" if self.pickup_target else "lowering"
            self.phase_time = 0.0
            self.message = "Picking"
            self.audio_events.append("button")
            self.descent_stall = 0.0
            self.previous_height = self.data.joint("z").qpos[0]

    def nudge(self, x, y):
        """Advance an axis target by one millimeter per key press."""
        if self.phase != "manual":
            return
        self.nudge_direction = (x, y)
        self.nudge_time = 0.12
        for axis, direction in (("x", x), ("y", y)):
            control = self.data.actuator(axis).ctrl
            low, high = self.model.actuator(axis).ctrlrange
            control[0] = min(high, max(low, control[0] + direction * 0.001))

    def step(self, x=0, y=0, z=0, fine=False, rotation=0):
        if self.phase != "manual":
            self.phase_time += self.model.opt.timestep
            if self.phase_time > 24:
                self.phase = "fault"
                self.message = "Motion timeout - press R"
            x = y = z = rotation = 0
            height = self.data.joint("z").qpos[0]
            low, high = self.model.actuator("z").ctrlrange
            if self.phase == "aligning":
                z = 1
                if height >= high - 0.006:
                    target = [
                        min(self.model.actuator(axis).ctrlrange[1],
                            max(self.model.actuator(axis).ctrlrange[0], value))
                        for axis, value in zip(
                            ("x", "y"), self.data.body(self.pickup_target).xpos[:2]
                        )
                    ]
                    for axis, value in zip(("x", "y"), target):
                        control = self.data.actuator(axis).ctrl
                        low_xy, high_xy = self.model.actuator(axis).ctrlrange
                        value = min(high_xy, max(low_xy, value))
                        travel = 0.04 * self.model.opt.timestep
                        control[0] += max(-travel, min(travel, value - control[0]))
                    if all(
                        abs(self.data.joint(axis).qpos[0] - value) < 0.001
                        for axis, value in zip(("x", "y"), target)
                    ):
                        self.phase = "lowering"
            elif self.phase == "lowering":
                z = -1
                if self.previous_height - height < 0.000005:
                    self.descent_stall += self.model.opt.timestep
                else:
                    self.descent_stall = 0.0
                self.previous_height = height
                if height <= low + 0.006 or self.descent_stall > 0.20:
                    z = 0
                    self.data.actuator("z").ctrl[0] = height
                    self.closed = True
                    self.audio_events.append("grip")
                    self.grip_time = 0.65
                    self.phase = "gripping"
            elif self.phase == "gripping":
                z = 0
                self.grip_time -= self.model.opt.timestep
                if self.grip_time <= 0:
                    self.phase = "raising"
            elif self.phase == "raising":
                z = 1
                if height >= high - 0.006:
                    self.data.actuator("z").ctrl[0] = high
                    self.grip_time = 0.12
                    self.phase = "settling"
            elif self.phase == "settling":
                self.grip_time -= self.model.opt.timestep
                if self.grip_time <= 0:
                    self.phase = "delivering"
            elif self.phase == "delivering":
                for axis, target in zip(("x", "y"), DROP_TARGET):
                    control = self.data.actuator(axis).ctrl
                    delta = target - control[0]
                    travel = 0.055 * self.model.opt.timestep
                    control[0] += max(-travel, min(travel, delta))
                arrived = all(
                    abs(self.data.joint(axis).qpos[0] - target) < 0.002
                    for axis, target in zip(("x", "y"), DROP_TARGET)
                )
                if arrived:
                    self.closed = False
                    self.grip_time = 2.0
                    self.phase = "releasing"
            elif self.phase == "releasing":
                self.grip_time -= self.model.opt.timestep
                if self.grip_time <= 0:
                    previous = len(self.collected)
                    for name in PRIZES:
                        px, py, pz = self.data.body(name).xpos
                        if (
                            abs(px - DROP_TARGET[0]) < 0.065
                            and -0.39 < py < -0.058
                            and -0.24 < pz < -0.035
                        ):
                            self.collected.add(name)
                    gained = len(self.collected) > previous
                    self.audio_events.append("success" if gained else "miss")
                    if gained:
                        self.celebration_until = self.data.time + 1.6
                    if len(self.collected) == len(PRIZES):
                        self.message = f"All {len(PRIZES)} collected! Press R to play again"
                    elif len(self.collected) > previous:
                        self.message = "Prize collected! Aim for the next one"
                    else:
                        self.message = "Missed - aim and try again"
                    self.phase = "manual"
                    self.phase_time = 0.0

        speed = 0.008 if fine else 0.075
        xy_scale = speed / max(1, hypot(x, y))
        velocities = (x * xy_scale, y * xy_scale, z * 0.085)
        for name, velocity in zip(AXES, velocities):
            actuator = self.model.actuator(name)
            control = self.data.actuator(name).ctrl
            low, high = actuator.ctrlrange
            target = control[0] + velocity * self.model.opt.timestep
            control[0] = min(high, max(low, target))
        yaw = self.data.actuator("yaw").ctrl
        low_yaw, high_yaw = self.model.actuator("yaw").ctrlrange
        angular_speed = 0.20 if fine else 0.85
        yaw[0] = min(high_yaw, max(
            low_yaw, yaw[0] + rotation * angular_speed * self.model.opt.timestep,
        ))
        for name in FINGERS:
            self.data.actuator(name).ctrl[0] = 0.95 if self.closed else 0
        # Display controls follow the user's manual input and the active pickup cycle.
        self.nudge_time = max(0, self.nudge_time - self.model.opt.timestep)
        stick_x, stick_y = (x, y)
        if self.phase == "manual" and self.nudge_time > 0:
            stick_x, stick_y = self.nudge_direction
        length = hypot(stick_x, stick_y)
        angle = min(1, length) * 0.35
        rotation = np.array([1.0, 0.0, 0.0, 0.0])
        if length:
            rotation[:] = [
                cos(angle / 2),
                -stick_y / length * sin(angle / 2),
                stick_x / length * sin(angle / 2),
                0,
            ]
        self.data.mocap_quat[self.model.body("panel_joystick").mocapid[0]] = rotation
        button = self.model.body("panel_button").mocapid[0]
        pressed = self.phase not in ("manual", "fault")
        self.data.mocap_pos[button, 2] = 0.019 - (0.005 if pressed else 0)
        mujoco.mj_step(self.model, self.data)
        self.animate_leds()


class BackgroundMusic:
    """Separate Windows MCI players let effects overlap the background loop."""

    def __init__(self):
        self.playing = False
        self.effects_on = True
        self.aliases = {}
        self.next_poll = 0.0
        self.send = None
        if sys.platform == "win32":
            self.send = ctypes.windll.winmm.mciSendStringW
            prefix = "pickdoll_" + uuid4().hex
            for name in ("theme", "button", "grip", "success", "miss"):
                filename = "pickdoll-theme.wav" if name == "theme" else name + ".wav"
                path = MODEL_PATH.parent / "assets" / filename
                alias = prefix + "_" + name
                if path.is_file() and self.command(
                    f'open "{path}" type waveaudio alias {alias}'
                )[0] == 0:
                    self.aliases[name] = alias

    def command(self, text):
        if self.send is None:
            return 1, ""
        result = ctypes.create_unicode_buffer(256)
        error = self.send(text, result, len(result), None)
        return error, result.value

    def toggle(self):
        if self.playing:
            self.stop()
        elif "theme" in self.aliases:
            error, _ = self.command(f"play {self.aliases['theme']} from 0")
            self.playing = error == 0
        return self.playing

    def poll(self):
        now = perf_counter()
        if self.playing and now >= self.next_poll:
            self.next_poll = now + 0.20
            error, mode = self.command(f"status {self.aliases['theme']} mode")
            if error:
                self.playing = False
            elif mode == "stopped":
                error, _ = self.command(f"play {self.aliases['theme']} from 0")
                self.playing = error == 0

    def play_effect(self, name):
        if self.effects_on and name in self.aliases:
            self.command(f"play {self.aliases[name]} from 0")

    def stop(self):
        if "theme" in self.aliases:
            self.command(f"stop {self.aliases['theme']}")
        self.playing = False

    def close(self):
        for alias in self.aliases.values():
            self.command(f"close {alias}")
        self.aliases.clear()
        self.playing = False


def add_celebration(claw, scene):
    remaining = claw.celebration_until - claw.data.time
    if remaining <= 0:
        return
    elapsed = 1.6 - remaining
    for index in range(20):
        angle = index * 2.39996
        radius = 0.025 + elapsed * (0.035 + (index % 4) * 0.01)
        position = np.array([
            radius * cos(angle),
            radius * sin(angle),
            0.16 + elapsed * 0.15 - elapsed * elapsed * 0.10,
        ])
        geom = scene.geoms[scene.ngeom]
        color = np.array(
            [1.0, 0.55 + (index % 3) * 0.17, 0.78, min(1, remaining)],
            dtype=np.float32,
        )
        mujoco.mjv_initGeom(
            geom, mujoco.mjtGeom.mjGEOM_SPHERE,
            np.array([0.0025, 0.0025, 0.0025]),
            position, np.eye(3).ravel(), color,
        )
        geom.emission = 1
        scene.ngeom += 1


def run():
    claw = Claw()
    if not glfw.init():
        raise RuntimeError("GLFW initialization failed.")
    context = None
    music = BackgroundMusic()
    try:
        window = glfw.create_window(940, 780, "PickDoll", None, None)
        if window is None:
            raise RuntimeError("Could not open the PickDoll window.")
        glfw.make_context_current(window)
        glfw.swap_interval(1)
        music.toggle()
        view_index = 0
        show_manual_controls = False
        camera_ids = [claw.model.camera(name).id for name, _ in VIEWS]

        def on_key(window, key, _scancode, action, _mods):
            nonlocal view_index, show_manual_controls
            if action != glfw.PRESS:
                return
            if _mods & glfw.MOD_CONTROL and key in (
                glfw.KEY_LEFT, glfw.KEY_RIGHT, glfw.KEY_UP, glfw.KEY_DOWN,
            ):
                horizontal = int(key == glfw.KEY_RIGHT) - int(key == glfw.KEY_LEFT)
                vertical = int(key == glfw.KEY_UP) - int(key == glfw.KEY_DOWN)
                claw.nudge(*aim_direction(VIEWS[view_index][0], horizontal, vertical))
            elif key == glfw.KEY_ENTER:
                claw.start_pickup()
            elif key == glfw.KEY_M:
                music.toggle()
            elif key == glfw.KEY_N:
                music.effects_on = not music.effects_on
            elif key == glfw.KEY_H:
                show_manual_controls = not show_manual_controls
            elif key == glfw.KEY_TAB:
                view_index = (view_index + 1) % len(VIEWS)
            elif glfw.KEY_1 <= key <= glfw.KEY_5:
                view_index = key - glfw.KEY_1
            elif key == glfw.KEY_SPACE and claw.phase == "manual":
                claw.closed = not claw.closed
            elif key == glfw.KEY_R:
                claw.reset()
            elif key == glfw.KEY_ESCAPE:
                glfw.set_window_should_close(window, True)

        glfw.set_key_callback(window, on_key)

        def pressed(key):
            return int(glfw.get_key(window, key) == glfw.PRESS)

        camera = mujoco.MjvCamera()
        camera.type = mujoco.mjtCamera.mjCAMERA_FIXED
        options = mujoco.MjvOption()
        options.geomgroup[4] = 1
        scene = mujoco.MjvScene(claw.model, maxgeom=500)
        context = mujoco.MjrContext(claw.model, mujoco.mjtFontScale.mjFONTSCALE_150)
        previous = perf_counter()
        lag = 0.0
        while not glfw.window_should_close(window):
            glfw.poll_events()
            now = perf_counter()
            lag += min(now - previous, 0.05)
            previous = now
            view_name, view_label = VIEWS[view_index]
            camera.fixedcamid = camera_ids[view_index]
            options.geomgroup[3] = view_name != "overhead"
            options.tendongroup[3] = view_name != "overhead"
            horizontal = pressed(glfw.KEY_RIGHT) - pressed(glfw.KEY_LEFT)
            vertical = pressed(glfw.KEY_UP) - pressed(glfw.KEY_DOWN)
            stepping = pressed(glfw.KEY_LEFT_CONTROL) or pressed(glfw.KEY_RIGHT_CONTROL)
            if stepping:
                horizontal = vertical = 0
            x, y = aim_direction(view_name, horizontal, vertical)
            z = pressed(glfw.KEY_E) - pressed(glfw.KEY_Q)
            rotation = pressed(glfw.KEY_X) - pressed(glfw.KEY_Z)
            fine = pressed(glfw.KEY_LEFT_SHIFT) or pressed(glfw.KEY_RIGHT_SHIFT)
            while lag >= claw.model.opt.timestep:
                claw.step(x, y, z, fine, rotation)
                lag -= claw.model.opt.timestep
            for event in claw.audio_events:
                music.play_effect(event)
            claw.audio_events.clear()
            music.poll()
            width, height = glfw.get_framebuffer_size(window)
            if width == 0 or height == 0:
                glfw.wait_events_timeout(0.05)
                continue
            viewport = mujoco.MjrRect(0, 0, width, height)
            mujoco.mjv_updateScene(
                claw.model, claw.data, options, None, camera,
                mujoco.mjtCatBit.mjCAT_ALL, scene,
            )
            x_pos, y_pos = claw.data.body("claw").xpos[:2]
            marker = scene.geoms[scene.ngeom]
            mujoco.mjv_initGeom(
                marker, mujoco.mjtGeom.mjGEOM_CYLINDER,
                np.array([0.012, 0.012, 0.0005]),
                np.array([x_pos, y_pos, 0.001]),
                np.eye(3).ravel(),
                np.array([0.58, 1.0, 0.39, 1.0], dtype=np.float32),
            )
            scene.ngeom += 1
            add_celebration(claw, scene)
            mujoco.mjr_render(viewport, scene, context)
            status = PHASE_LABELS.get(claw.phase, claw.message)
            mujoco.mjr_overlay(
                mujoco.mjtFont.mjFONT_NORMAL,
                mujoco.mjtGridPos.mjGRID_TOPLEFT,
                viewport,
                "PICKDOLL\nCollected\nCamera\nMusic\nStatus",
                f"\n{len(claw.collected)} / {len(PRIZES)}\n{view_label}\n{'On' if music.playing else 'Off'}\n{status}",
                context,
            )
            help_text = (
                "Arrow keys: aim   |   Hold Shift: fine aim\n"
                "Ctrl + Arrow: 1 mm   |   Z / X: rotate   |   Enter: pick\n"
                "Tab / 1-5: camera   |   R: restart   |   M: music   |   N: effects   |   H: help   |   Esc: exit"
            )
            if show_manual_controls:
                help_text += "\nQ / E: lower / raise   |   Space: open / close (when ready)"
            mujoco.mjr_overlay(
                mujoco.mjtFont.mjFONT_NORMAL,
                mujoco.mjtGridPos.mjGRID_BOTTOMLEFT,
                viewport, help_text, "", context,
            )
            glfw.swap_buffers(window)
    finally:
        music.close()
        if context is not None:
            context.free()
        glfw.terminate()


if __name__ == "__main__":
    run()
