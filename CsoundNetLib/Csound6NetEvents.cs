using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
/*
 * C S O U N D 6 N E T
 * Dot Net Wrappers for building C#/VB hosts for Csound 6 via the Csound API
 * and is licensed under the same terms and disclaimers as Csound indicates below.
 * Copyright (C) 2013 Richard Henninger
 *
 * C S O U N D
 *
 * An auto-extensible system for making music on computers
 * by means of software alone.
 *
 * Copyright (C) 2001-2013 Michael Gogins, Matt Ingalls, John D. Ramsdell,
 *                         John P. ffitch, Istvan Varga, Victor Lazzarini,
 *                         Andres Cabrera and Steven Yi
 *
 * This software is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This software is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this software; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 *
 */

namespace csound6netlib
{
    /**
     * \ingroup MESSAGES
     */
    /// <summary>
    /// Carries information supplied by Csound when it emits a message by calling
    /// csoundMessage and its peers internally.
    /// Message callbacks are converted into .net events using this class to convey message content.
    /// Message attributes, if provided, are translated into enum values and message type.
    /// </summary>
    public class Csound6MessageEventArgs : EventArgs
    {
        public Csound6MessageEventArgs()
            : this(MessageAttributes.Default, string.Empty)
        {
        }

        /// <summary>
        /// Creates a simple message with default (neutral) attribute information.
        /// </summary>
        /// <param name="message">the text to display</param>
        public Csound6MessageEventArgs(string message) : this(MessageAttributes.Default, message)
        {
        }

        /// <summary>
        /// Creates a message converting raw csound message attributes into various
        /// property enums conveying equivalent information.
        /// </summary>
        /// <param name="attrs"></param>
        /// <param name="msg"></param>
        public Csound6MessageEventArgs(MessageAttributes attrs, string msg)
        {
            Message = msg;
            uint attr = (uint)attrs;
            Type = (MessageType)((attr & 0x7000) >> 12);
            Bold = (attrs & MessageAttributes.Bold) != 0;
            Foreground = ((attr & 0x0100) != 0) ? (MessageColor)(attr & 0x0007) : MessageColor.Default;
            Background = ((attr & 0x0200) != 0) ? (MessageColor)((attr >> 4) & 0x0007) : MessageColor.Default;
            Underline = (attrs.HasFlag(MessageAttributes.Underline));

        }

        public MessageType Type;
        public MessageColor Foreground;
        public MessageColor Background;
        public bool Bold;
        public bool Underline;
        public string Message;
    }

    /**
     * \ingroup MESSAGES
     */
    /// <summary>
    /// C# style event handler for processing state messages from csound as they occur.
    /// Register your handler with Csound6Net.Csound6MessageOccurred += handler;
    /// </summary>
    /// <param name="sender">originator of event (can cast to Csound6Net)</param>
    /// <param name="args">object containing message text, type, and suggested attributes</param>
    public delegate void Csound6MessageEventHandler(object sender, Csound6MessageEventArgs e);

   /**
    * \ingroup GENERALIO
    */
    /// <summary>
    /// Supplies the information conveyed in a csound file open callback as an
    /// argument object to be used in converting that callback into a .net event.
    /// </summary>
    public class Csound6FileOpenEventArgs : EventArgs
    {
        public string Path;
        public CsfType FileType;
        public bool IsWriting;
        public bool IsTemporary;
    }


    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void Csound6FileOpenEventHandler(object sender, Csound6FileOpenEventArgs e);

    /**
     * \ingroup REALTIME
     */
    /// <summary>
    /// 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    public delegate void Csound6RtcloseEventHandler(object sender, EventArgs e);

    /// <summary>
    /// Supports InputChannelEvents and OutputChannelEvents by conveying information
    /// to/from csound "invalue" and "outvalue" opcodes into .net events.
    /// When received during an OutputChannelEvent, the Value property is the value
    /// reported by an instrument's "outvalue" opcode.
    /// When received during an InputChannelEvent, use the SetCsoundValue() method to
    /// communicate a new value to csound.
    /// </summary>
    public class Csound6ChannelEventArgs : EventArgs
    {
        private IntPtr m_pObject;
        public Csound6ChannelEventArgs()
        {
        }

        public Csound6ChannelEventArgs(string name, ChannelType type, ChannelDirection direction)
            : this(name, type, direction, IntPtr.Zero)
        {
        }

        public Csound6ChannelEventArgs(string _name, ChannelType _type, ChannelDirection _direction, IntPtr pObject)
        {
            Name = _name;
            Type = _type;
            Direction = _direction;
            m_pObject = pObject;
        }

        public string Name;
        public object Value;
        public ChannelType Type;
        public ChannelDirection Direction;

        /// <summary>
        /// Supports responding to an InputChannelCallback event by providing a method to
        /// communicate a value back to csound appropriate to the channel.
        /// The "invalue" opcode, which is the only opcode to trigger this event,
        /// only supports receiving strings and MYFLT (double) control values.
        /// </summary>
        /// <param name="value"></param>
        public void SetCsoundValue(Csound6NetRealtime csound, object value)
        {
            if (m_pObject != IntPtr.Zero)
            {
                switch (Type)
                {
                    case ChannelType.Control:
                        Marshal.StructureToPtr((double)value, m_pObject, false);
                        break;
                    case ChannelType.String:
                        byte[] buf = new byte[Csound6Channel.GetChannelDataSize(csound, Name)];
                        string s = value.ToString();
                        if (s.Length + 1 > buf.Length) s = s.Substring(0, buf.Length - 1);
                        ASCIIEncoding.ASCII.GetBytes(s, 0, s.Length, buf, 0);
                        buf[s.Length + 1] = 0;
                        Marshal.Copy(buf, 0, m_pObject, buf.Length);
                        break;
                    default:  //other types not supported in csound: pvs, audio, var
                        break;
                }
            }
        }
    }

    public class Csound6SenseEventsArgs : EventArgs
    {
        public object UserData;
    }


    public delegate void Csound6ChannelEventHandler(object sender, Csound6ChannelEventArgs e);

    public delegate void Csound6SenseEventCallbackHandler(object sender, Csound6SenseEventsArgs e);

}
