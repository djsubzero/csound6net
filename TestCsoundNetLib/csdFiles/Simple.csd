<CsoundSynthesizer>
<CsOptions>
-R -f -W -o simple.wav
</CsOptions>
<CsInstruments>
sr = 44100
kr = 441
ksmps = 100
nchnls = 1
0dbfs = 1.0

	instr 1
	a1 oscil p4, p5, 1
	out a1
	endin

</CsInstruments>
<CsScore>
f1 0	2048	10	1

i 1	0	1	10000	440.0
e
</CsScore>
</CsoundSynthesizer>
