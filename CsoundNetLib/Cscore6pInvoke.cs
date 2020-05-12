using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using System.ComponentModel;

namespace csound6netlib
{
    public partial class Cscore6
    {

        private class NativeMethods
        {
            /* Prepares an instance of Csound for Cscore processing outside of running an orchestra (i.e. "standalone Cscore").
             * It is an alternative to csoundPreCompile(), csoundCompile(), and csoundPerform*()
             * and should not be used with these functions.
             * You must call this function before using the interface in "cscore.h" when you do not wish
             * to compile an orchestra.
             * Pass it the already open FILE* pointers to the input and output score files.
             */
            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern Int32 csoundInitializeCscore(IntPtr csound, IntPtr ifile, IntPtr ofile);

            /* Sets an external callback for Cscore processing.
             * Pass NULL to reset to the internal cscore() function (which does nothing).
             * This callback is retained after a csoundReset() call.
             */
            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void csoundSetCscoreCallback(IntPtr csound, CscoreCallback cscCallback);
        }
        //TODO: need to make small bridge c library to pass file names into fopen etc for cscore to work
/*
 * The three structures and prototypes for cscore
        typedef struct cshdr {
        struct cshdr *prvblk;
        struct cshdr *nxtblk;
        int16  type;
        int16  size;
} CSHDR;

// Single score event structure

typedef struct {
        CSHDR h;
        char  *strarg;
        char  op;
        int16 pcnt;
        MYFLT p2orig;
        MYFLT p3orig;
        MYFLT p[1];
} EVENT;

// Event list structure
typedef struct {
        CSHDR h;
        int   nslots;
        int   nevents;
        EVENT *e[1];
} EVLIST;

// This pragma must come before all public function declarations
#if (defined(macintosh) && defined(__MWERKS__))
#  pragma export on
#endif

// Functions for working with single events
PUBLIC EVENT  *cscoreCreateEvent(CSOUND*, int);
PUBLIC EVENT  *cscoreDefineEvent(CSOUND*, char*);
PUBLIC EVENT  *cscoreCopyEvent(CSOUND*, EVENT*);
PUBLIC EVENT  *cscoreGetEvent(CSOUND*);
PUBLIC void    cscorePutEvent(CSOUND*, EVENT*);
PUBLIC void    cscorePutString(CSOUND*, char*);

//  Functions for working with event lists 
PUBLIC EVLIST *cscoreListCreate(CSOUND*, int);
PUBLIC EVLIST *cscoreListAppendEvent(CSOUND*, EVLIST*, EVENT*);
PUBLIC EVLIST *cscoreListAppendStringEvent(CSOUND*, EVLIST*, char*);
PUBLIC EVLIST *cscoreListGetSection(CSOUND*);
PUBLIC EVLIST *cscoreListGetNext(CSOUND *, MYFLT);
PUBLIC EVLIST *cscoreListGetUntil(CSOUND*, MYFLT);
PUBLIC EVLIST *cscoreListCopy(CSOUND*, EVLIST*);
PUBLIC EVLIST *cscoreListCopyEvents(CSOUND*, EVLIST*);
PUBLIC EVLIST *cscoreListExtractInstruments(CSOUND*, EVLIST*, char*);
PUBLIC EVLIST *cscoreListExtractTime(CSOUND*, EVLIST*, MYFLT, MYFLT);
PUBLIC EVLIST *cscoreListSeparateF(CSOUND*, EVLIST*);
PUBLIC EVLIST *cscoreListSeparateTWF(CSOUND*, EVLIST*);
PUBLIC EVLIST *cscoreListAppendList(CSOUND*, EVLIST*, EVLIST*);
PUBLIC EVLIST *cscoreListConcatenate(CSOUND*, EVLIST*, EVLIST*);
PUBLIC void    cscoreListPut(CSOUND*, EVLIST*);
PUBLIC int     cscoreListPlay(CSOUND*, EVLIST*);
PUBLIC void    cscoreListSort(CSOUND*, EVLIST*);
PUBLIC int     cscoreListCount(CSOUND*, EVLIST *);

//* Functions for reclaiming memory
PUBLIC void    cscoreFreeEvent(CSOUND*, EVENT*);
PUBLIC void    cscoreListFree(CSOUND*, EVLIST*);
PUBLIC void    cscoreListFreeEvents(CSOUND*, EVLIST*);

// Functions for working with multiple input score files 
PUBLIC FILE   *cscoreFileOpen(CSOUND*, char*);
PUBLIC void    cscoreFileClose(CSOUND*, FILE*);
PUBLIC FILE   *cscoreFileGetCurrent(CSOUND*);
PUBLIC void    cscoreFileSetCurrent(CSOUND*, FILE*);
    */
    }
}
