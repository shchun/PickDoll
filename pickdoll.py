"""Interactive PickDoll simulator with automatic pickup and delivery."""

from math import hypot
from pathlib import Path
from time import perf_counter

import glfw
import mujoco
import numpy as np


MODEL_PATH = Path(__file__).with_name("claw.xml")
AXES = ("x", "y", "z")
PRIZES = ("prize", "prize_left", "prize_right")
DROP_TARGET = (-0.128, -0.128)
FINGERS = ("finger_a", "finger_b", "finger_c")
PHASE_LABELS = {
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
        self.reset()

    def reset(self):
        mujoco.mj_resetData(self.model, self.data)
        self.closed = False
        self.phase = "manual"
        self.grip_time = 0.0
        self.phase_time = 0.0
        self.collected = set()
        self.message = "Ready"
        mujoco.mj_forward(self.model, self.data)

    def start_pickup(self):
        if self.phase == "manual" and len(self.collected) < len(PRIZES):
            self.closed = False
            self.phase = "lowering"
            self.phase_time = 0.0
            self.message = "Picking"

    def step(self, x=0, y=0, z=0, fine=False):
        if self.phase != "manual":
            self.phase_time += self.model.opt.timestep
            if self.phase_time > 24:
                self.phase = "fault"
                self.message = "Motion timeout - press R"
            x = y = z = 0
            height = self.data.joint("z").qpos[0]
            low, high = self.model.actuator("z").ctrlrange
            if self.phase == "lowering":
                z = -1
                if height <= low + 0.006:
                    self.closed = True
                    self.grip_time = 0.35
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
                            and abs(py - DROP_TARGET[1]) < 0.065
                            and -0.17 < pz < -0.035
                        ):
                            self.collected.add(name)
                    if len(self.collected) == len(PRIZES):
                        self.message = "All 3 collected! Press R to play again"
                    elif len(self.collected) > previous:
                        self.message = "Prize collected! Aim for the next one"
                    else:
                        self.message = "Missed - aim and try again"
                    self.phase = "manual"
                    self.phase_time = 0.0

        speed = 0.021 if fine else 0.075
        xy_scale = speed / max(1, hypot(x, y))
        velocities = (x * xy_scale, y * xy_scale, z * 0.085)
        for name, velocity in zip(AXES, velocities):
            actuator = self.model.actuator(name)
            control = self.data.actuator(name).ctrl
            low, high = actuator.ctrlrange
            target = control[0] + velocity * self.model.opt.timestep
            control[0] = min(high, max(low, target))
        for name in FINGERS:
            self.data.actuator(name).ctrl[0] = 0.95 if self.closed else 0
        mujoco.mj_step(self.model, self.data)


def run():
    claw = Claw()
    if not glfw.init():
        raise RuntimeError("GLFW initialization failed.")
    context = None
    try:
        window = glfw.create_window(940, 780, "PickDoll", None, None)
        if window is None:
            raise RuntimeError("Could not open the PickDoll window.")
        glfw.make_context_current(window)
        glfw.swap_interval(1)
        view_index = 0
        show_manual_controls = False
        camera_ids = [claw.model.camera(name).id for name, _ in VIEWS]

        def on_key(window, key, _scancode, action, _mods):
            nonlocal view_index, show_manual_controls
            if action != glfw.PRESS:
                return
            if key == glfw.KEY_ENTER:
                claw.start_pickup()
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
            x, y = aim_direction(view_name, horizontal, vertical)
            z = pressed(glfw.KEY_E) - pressed(glfw.KEY_Q)
            fine = pressed(glfw.KEY_LEFT_SHIFT) or pressed(glfw.KEY_RIGHT_SHIFT)
            while lag >= claw.model.opt.timestep:
                claw.step(x, y, z, fine)
                lag -= claw.model.opt.timestep
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
            mujoco.mjr_render(viewport, scene, context)
            status = PHASE_LABELS.get(claw.phase, claw.message)
            mujoco.mjr_overlay(
                mujoco.mjtFont.mjFONT_NORMAL,
                mujoco.mjtGridPos.mjGRID_TOPLEFT,
                viewport,
                "PICKDOLL\nCollected\nCamera\nStatus",
                f"\n{len(claw.collected)} / 3\n{view_label}\n{status}",
                context,
            )
            help_text = (
                "Arrow keys: aim   |   Hold Shift: fine aim\n"
                "Enter: pick and deliver   |   Tab / 1-5: camera\n"
                "R: restart   |   H: manual controls   |   Esc: exit"
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
        if context is not None:
            context.free()
        glfw.terminate()


if __name__ == "__main__":
    run()
