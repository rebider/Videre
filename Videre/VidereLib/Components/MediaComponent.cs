﻿using System;
using System.IO;
using VidereLib.EventArgs;

namespace VidereLib.Components
{
    public class MediaComponent : ComponentBase
    {
        /// <summary>
        /// Returns whether or not any media has been loaded.
        /// </summary>
        public bool HasMediaBeenLoaded { private set; get; }

        /// <summary>
        /// Gets called whenever media has been loaded.
        /// </summary>
        public event EventHandler<OnMediaLoadedEventArgs> OnMediaLoaded;

        /// <summary>
        /// Gets called whenever media has been unloaded.
        /// </summary>
        public event EventHandler<OnMediaUnloadedEventArgs> OnMediaUnloaded; 

        public MediaComponent( ViderePlayer player ) : base( player )
        {
        }

        public TimeSpan GetMediaLength( )
        {
            if ( !HasMediaBeenLoaded )
                throw new Exception( "Attempted to get media length while there is no media loaded." );

            return Player.windowData.MediaPlayer.NaturalDuration.TimeSpan;
        }

        /// <summary>
        /// Loads media from a path.
        /// </summary>
        /// <param name="Path">The path of the media.</param>
        public void LoadMedia( string Path )
        {
            if ( Player.GetComponent<StateComponent>( ).CurrentState != StateComponent.PlayerState.Stopped )
                throw new Exception( "Attempting to load media while playing." );

            FileInfo info = new FileInfo( Path );

            Player.windowData.MediaPlayer.Source = new Uri( info.FullName );
            HasMediaBeenLoaded = true;
            OnMediaLoaded?.Invoke( this, new OnMediaLoadedEventArgs( info ) );
        }

        /// <summary>
        /// Unloads the currently loaded media.
        /// </summary>
        public void UnloadMedia( )
        {
            HasMediaBeenLoaded = false;
            OnMediaUnloaded?.Invoke( this, new OnMediaUnloadedEventArgs( ) );
        }
    }
}