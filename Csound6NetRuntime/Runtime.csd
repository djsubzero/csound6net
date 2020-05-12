<CsoundSynthesizer>
<CsOptions>
	-o simple.wav
</CsOptions>
<CsInstruments>
sr = 44100
kr = 4410
ksmps = 10
nchnls = 1


	instr 1
	kfreq chnget "pitch"  ; tracks Hz as set by client program using "pitch" control channel to a value
	asig oscil p4, kfreq, 1
	out asig
	endin

</CsInstruments>
<CsScore>
f1 0	8192	10	1
i1 0 60 10000 ; play sine for a minute at most recent frequency in channel named "pitch"

e
</CsScore>
</CsoundSynthesizer>
