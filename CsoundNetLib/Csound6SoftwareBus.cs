using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 * C S O U N D 6 N E T
 * Dot Net Wrappers for building C#/VB hosts for Csound 6 via the Csound API
 * and is licensed under the same terms and disclaimers as Csound described below.
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

    /// <summary>
    /// Provides a convenient way to interact with csound's channel system.
    /// Upon creation, all channels csound already knows about (usually after
    /// compiling an orchestra) are given channel objects of the appropriate type
    /// to facilitate client software's interaction with csound.
    /// The bus's indexer aims at the channel's value thus allowing simple interaction
    /// with a channel's content: object value = bus[name]
    /// or bus[name] = value where value is presumed an instance
    /// (boxed for control channels) of data appropriate for that bus.
    /// To obtain an actual channel object rather than its value, use the GetChannel(name) method.
    /// </summary>
    public class Csound6SoftwareBus : IDisposable
    {

        private IDictionary<string, Csound6Channel> m_channels;
        private Csound6NetRealtime m_csound;

        /**
         * \addtogroup CHANNELS
         * @{
         */
        /// <summary>
        /// Creates a software bus already loaded with all channels known to csound.
        /// This is only useful after having compiled an orchestra.
        /// If created before compiling, the bus will be empty, but can be filled later 
        /// by calling its Refresh() method.
        /// </summary>
        /// <param name="csound">the instance of csound to associate with this bus.</param>
        public Csound6SoftwareBus(Csound6NetRealtime csound)
        {
            m_csound = csound;
            m_channels = new Dictionary<string, Csound6Channel>();
            Refresh();
        }

        ~Csound6SoftwareBus()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            foreach (var nvpair in m_channels)
            {
                var chan = nvpair.Value;
                chan.Dispose();//release channel's unmanaged memory
            }
            m_channels.Clear();
            m_csound = null;
        }

        /// <summary>
        /// Create and add a channel to the bus based upon the content of a channel info object.
        /// Used by Refresh as it scans csound's defined channels.
        /// If the bus already has a channel with the same name, a new channel is created based upon
        /// ChannelInfo data and the old channel object (not the channel in csound) is discarded.
        /// Obviously, channel types in csound must match the type represented in the bus.
        /// Use more specific AddChannel methods to create channels from client software.
        /// </summary>
        /// <param name="info">a ChannelInfo object as returned by csound's GetChannelList() method</param>
        /// <returns>a reference to the channel object added or modified in the bus</returns>
        public Csound6Channel AddChannel(ChannelInfo info)
        {
            if (!HasChannel(info.Name)) m_channels.Add(info.Name, CreateChannelFromInfo(info));
            else if (m_channels[info.Name].Type != info.Type) m_channels[info.Name] = CreateChannelFromInfo(info);
            return m_channels[info.Name];
        }

        /// <summary>
        /// Adds a string channel object to the bus but doesn't define it to csound.
        /// </summary>
        /// <param name="name">name to use for this channel</param>
        /// <param name="direction">input or output or both (or'd together)</param>
        public Csound6StringChannel AddStringChannel(string name, ChannelDirection direction)
        {
            if (HasChannel(name) && (m_channels[name].Type != ChannelType.String)) RemoveChannel(name);
            m_channels.Add(name, new Csound6StringChannel(name, direction, m_csound));
            m_channels[name].Direction = direction;
            return m_channels[name] as Csound6StringChannel;
        }

        /// <summary>
        /// Adds a control channel object to the bus but doesn't define it to csound
        /// </summary>
        /// <param name="name"></param>
        /// <param name="direction"></param>
        public Csound6ControlChannel AddControlChannel(string name, ChannelDirection direction)
        {
            if (HasChannel(name)  && (m_channels[name].Type != ChannelType.Control)) RemoveChannel(name);
            m_channels.Add(name, new Csound6ControlChannel(name, direction, m_csound));
            m_channels[name].Direction = direction;
            return m_channels[name] as Csound6ControlChannel;
        }

        /// <summary>
        /// Adds an audio channel object to the bus but doesn't define it to csound
        /// </summary>
        /// <param name="name"></param>
        /// <param name="direction"></param>
        public Csound6AudioChannel AddAudioChannel(string name, ChannelDirection direction)
        {
            if (HasChannel(name) && (m_channels[name].Type != ChannelType.Audio)) RemoveChannel(name);
            m_channels.Add(name, new Csound6AudioChannel(name, direction, m_csound));
            m_channels[name].Direction = direction;
            return m_channels[name] as Csound6AudioChannel;
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="name"></param>
        ///// <param name="direction"></param>
        //public void AddPvsChannel(string name, ChannelDirection direction)
        //{//TODO: implement when pvs type established
        //}

        /// <summary>
        /// Provides an enumerable collection of Csound6Channel objects currently defined to the bus.
        /// Each channel's type (subclass) can be determined by the Type attribute.
        /// </summary>
        public ICollection<Csound6Channel> Channels { get { return m_channels.Values; } }

        /// <summary>
        /// Indicates how many Csound6Channel objects are known to the bus.
        /// </summary>
        public int Count { get { return m_channels.Count; } }

        /// <summary>
        /// Indicates whether the bus contains a channel with the requested name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool HasChannel(string name)
        {
            return m_channels.ContainsKey(name);
        }

        /// <summary>
        /// Provides the channel object associated with the the provided name
        /// </summary>
        /// <param name="name">the name of the channel to return</param>
        /// <returns>the channel known by the requested name or null if it there is none by that name</returns>
        public Csound6Channel GetChannel(string name) { return (HasChannel(name)) ? m_channels[name] : null; }

        /// <summary>
        /// Provides an enumerable collection of all known channels of the requested channel type.
        /// </summary>
        /// <param name="type">the type of csound channel to return</param>
        /// <returns>a list of channels of the requested type</returns>
        public ICollection<Csound6Channel> GetChannelsOfType(ChannelType type)
        {
            var list = new List<Csound6Channel>();
            foreach (var channel in Channels)
            {
                if (channel.Type == type) list.Add(channel);
            }
            return list;
        }

        /// <summary>
        /// Gets or sets the data value of the channel known by the provided name indexer.
        /// For the getter, the type of the object returned is of the type appropriate for the requested channel.
        /// If the setter object is inappropriate for the requested channel, a cast exception would be thrown.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="System.InvalidCastException"></exception>
        public object this[string name]
        {
            get {
                    return HasChannel(name) ? m_channels[name].Value : null;
                }
            set
            {
                if (!HasChannel(name))
                {
                    if (value is double)
                    {
                        AddControlChannel(name, ChannelDirection.Input | ChannelDirection.Output);
                    }
                    else if (value is string)
                    {
                        AddStringChannel(name, ChannelDirection.Input | ChannelDirection.Output);
                    }
                }
                m_channels[name].Value = value;
            }
        }

        /// <summary>
        /// Queries csound for a list of existing channels based upon declarations it has encountered
        /// and makes sure that each entry is represented in the software bus's registry.
        /// </summary>
        public void Refresh()
        {
            var channels = m_csound.GetChannelList();
            if ((channels != null) && (channels.Count > 0))
            {
                foreach (var chnl in channels)
                {
                    AddChannel(chnl.Value);
                }
            }
        }

        /// <summary>
        /// Removes from the software bus the channel object with the provided name.
        /// It is not deleted from csound, however.
        /// </summary>
        /// <param name="name">name of the channel to remove from the bus's collection</param>
        /// <returns>the removed channel or null when there is no channel with the provided name</returns>
        public Csound6Channel RemoveChannel(string name)
        {
            Csound6Channel channel = null;
            if (HasChannel(name))
            {
                channel = m_channels[name];
                m_channels.Remove(name);
            }
            return channel;
        }

        /*
         * @}
         */

        /// <summary>
        /// Constructs the appropriate channel subclass based on the properties of the provided ChannelInfo object.
        /// Used primarily by AddChannel as called from Refresh().
        /// </summary>
        /// <param name="info">the ChannelInfo object from which to construct a channel object</param>
        /// <returns></returns>
        private Csound6Channel CreateChannelFromInfo(ChannelInfo info)
        {
            Csound6Channel channel = null;
            switch (info.Type)
            {
                case ChannelType.Control:
                    channel = new Csound6ControlChannel(info.Name, info.Direction, m_csound);
                    // info.Hints;
                    break;
                case ChannelType.String:
                    channel = new Csound6StringChannel(info.Name, info.Direction, m_csound);
                    break;
                case ChannelType.Audio:
                    channel = new Csound6AudioChannel(info.Name, info.Direction, m_csound);
                    break;
                case ChannelType.Pvs:
//                    channel = new Csound6PvsChannel(info.Name, info.Direction, m_csound);//not supported yet
                    break;
                case ChannelType.Var:

                default:
                    break;
            }
            return channel;
        }

     }
}
