"""Two original, seamless 16-second electronic cues. No sampled recordings."""
import math,random,wave,struct,pathlib
root=pathlib.Path(__file__).resolve().parents[1]/'Assets/CostaBrava/Audio'
rate=22050;duration=16;random.seed(14)
for name,intense in [('coast',False),('pursuit',True)]:
 samples=bytearray();tempo=120;beat=60/tempo
 notes=[110,130.8128,164.8138,146.8324]
 for i in range(rate*duration):
  t=i/rate;b=int(t/beat);phase=t%beat;freq=notes[(b//8)%4]
  chord=sum(math.sin(math.tau*freq*r*t) for r in [1,1.5,2])/3*.08
  bass=math.sin(math.tau*freq*.5*t)*math.exp(-phase*7)*.18
  arp=math.sin(math.tau*freq*[2,3,4,3][int(t/(beat/2))%4]*t)*math.exp(-(t%(beat/2))*13)*.055
  kick=math.sin(math.tau*(45*phase+2.4*(1-math.exp(-phase*30))))*math.exp(-phase*24)*.27
  snare=(random.random()*2-1)*math.exp(-phase*32)*(.12 if b%2 else 0)
  hat=(random.random()*2-1)*math.exp(-(t%(beat/2))*80)*(.045 if intense else .02)
  v=chord+bass+arp+kick+snare+hat
  if intense:v+=math.sin(math.tau*freq*4*t)*math.exp(-(t%(beat/4))*20)*.04
  samples.extend(struct.pack('<h',int(max(-1,min(1,v))*26000)))
 with wave.open(str(root/(name+'.wav')),'wb') as out:out.setnchannels(1);out.setsampwidth(2);out.setframerate(rate);out.writeframes(samples)
print('Original music exported')
