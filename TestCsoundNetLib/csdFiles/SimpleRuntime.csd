<CsoundSynthesizer>
<CsOptions>
-R -W -o simple.wav
</CsOptions>
<CsInstruments>
sr = 44100
ksmps = 4410  ;size tests expect ksmps to be 4410. If change, change tests which use ksmps as multiplier
nchnls = 1

chn_k "chan1", 1, 3, 1000, 500, 2000
chn_k "chan2", 3, 1, 100, 50, 500, 0, 0, 10, 10, "a"
chn_a "achan1", 2
chn_a "achan2", 3
chn_a "achan3", 3
chn_S "schan1", 3
chn_S "schan2", 1
chn_S "schan3", 3

; Instrument exercises input/output callbacks set via invalue and outvalue.
; If you change values, be sure to change tests which verify channel contents: they have expected values from each other

	instr 1
    kMoveUp  line 0.01, p3, 1.0
    outvalue "chan2", kMoveUp
    Stest    sprintfk "Csound 6 is cool! value=%f", kMoveUp
    outvalue "schan1", Stest
    ktest invalue "chan1"
    Sintest invalue "schan2"
    Sresult sprintfk "%s=%f", Sintest, ktest
    outvalue "schan3", Sresult
	endin
  
 instr 2
 fsig pvsinit 1024 
; pvsout fsig, 0
 endin

  instr 3
    a1 oscil p4, p5, 1
    chnset a1, "achan1"
    a2 chnget "achan2"
    chnset a2, "achan3"
  endin

</CsInstruments>
<CsScore>
f1 0	4096	10	1

i1 0 2
;i2 0 0.02
i3 0.1 .3 20000 441  ;will fill achan1 with sinewave at 100 samples per cycle
e
</CsScore>
</CsoundSynthesizer>
