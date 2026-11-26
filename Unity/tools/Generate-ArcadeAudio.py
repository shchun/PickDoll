"""Generate the original PickDoll arcade sound bank (numpy/scipy)."""
from pathlib import Path
import wave
import numpy as np
from scipy.signal import butter, sosfilt
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'Assets/PickDoll/Audio'
OUT.mkdir(parents=True,exist_ok=True)
RATE=44100
rng=np.random.default_rng(1101)
def note(freq,duration=.28,gain=.3,pan=0,decay=13):
 t=np.arange(int(RATE*duration))/RATE
 pitch=freq*(1-.07*np.exp(-t*70))
 phase=2*np.pi*np.cumsum(pitch)/RATE
 env=(1-np.exp(-t*650))*np.exp(-t*decay)*np.minimum(1,(duration-t)/.025)
 body=np.sin(phase)+.18*np.sin(2*phase)+.065*np.sin(3*phase)
 knock=.12*np.sin(phase*3.98)*np.exp(-t*70)
 signal=(body+knock)*env*gain
 return np.column_stack((signal*np.cos((pan+1)*np.pi/4),signal*np.sin((pan+1)*np.pi/4)))
def tune(events,length):
 data=np.zeros((int(length*RATE),2))
 for time,freq,gain,duration,pan in events:
  a=note(freq,duration,gain,pan,10 if duration>.35 else 16);i=int(time*RATE);n=min(len(a),len(data)-i);data[i:i+n]+=a[:n]
 return data
def pop(start,end,length,gain=.28):
 t=np.arange(int(length*RATE))/RATE
 phase=2*np.pi*np.cumsum(end+(start-end)*np.exp(-t*24))/RATE
 env=(1-np.exp(-t*900))*np.exp(-t*(12/length)*.12)*np.minimum(1,(length-t)/.015)
 x=(np.sin(phase)+.1*np.sin(2*phase))*env*gain
 return np.column_stack((x,x))
bank={
 'ui-pop':pop(650,380,.085,.24),
 'grab-boing':pop(510,250,.24,.30),
 'clamp-tap':pop(300,180,.12,.24),
 'release-plop':pop(730,330,.18,.22),
 'collect':tune([(0,523.25,.25,.24,-.12),(.085,659.25,.24,.24,.12),(.17,783.99,.25,.28,-.10),(.30,1046.5,.29,.42,.10),(.30,523.25,.11,.4,0)],.82),
 'combo':tune([(0,659.25,.24,.24,-.15),(.085,783.99,.24,.26,.15),(.17,1046.5,.26,.3,-.1),(.285,1318.51,.22,.3,.1),(.42,1046.5,.29,.52,0),(.42,659.25,.16,.48,-.12),(.42,783.99,.13,.48,.12),(.42,261.63,.17,.36,0)],1.02),
 'restock':tune([(0,392,.25,.28,-.18),(.12,523.25,.24,.28,.18),(.24,659.25,.25,.28,-.1),(.36,783.99,.23,.3,.1),(.50,1046.5,.29,.4,0),(.50,523.25,.12,.35,0)],.96),
}
def write(path,data):
 data=sosfilt(butter(2,6500,fs=RATE,output='sos'),data,axis=0)
 data=np.tanh(data*1.12)
 peak=np.max(np.abs(data))
 if peak>0:data*=.72/peak
 data[-int(RATE*.015):]*=np.linspace(1,0,int(RATE*.015))[:,None]
 with wave.open(str(path),'wb') as w:w.setnchannels(2);w.setsampwidth(2);w.setframerate(RATE);w.writeframes((data*32767).astype('<i2').tobytes())
for name,data in bank.items():write(OUT/(name+'.wav'),data)
preview=np.concatenate([bank['collect'],np.zeros((int(RATE*.35),2)),bank['combo'],np.zeros((int(RATE*.35),2)),bank['restock']])
preview_path=ROOT.parent/'docs/android/media/week-11/arcade-preview.wav'
write(preview_path,preview)
print('Generated',len(bank),'effects; preview:',preview_path)
