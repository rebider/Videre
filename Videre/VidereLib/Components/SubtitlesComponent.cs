﻿using System;
using System.Windows.Threading;
using VidereLib.EventArgs;

namespace VidereLib.Components
{
    /// <summary>
    /// The subtitles component.
    /// </summary>
    public class SubtitlesComponent : ComponentBase
    {
        /// <summary>
        /// Gets called whenever the current subtitles changed.
        /// </summary>
        public event EventHandler<OnSubtitlesChangedEventArgs> OnSubtitlesChanged;

        /// <summary>
        /// The subtitles data currently loaded.
        /// </summary>
        public Subtitles Subtitles { private set; get; }
        private TimeSpan subtitlesOffset;

        private DispatcherTimer subtitlesTimer;

        /// <summary>
        /// Returns whether or not subtitles have been loaded.
        /// </summary>
        public bool HaveSubtitlesBeenLoaded => Subtitles != null;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="player">The <see cref="ViderePlayer"/>.</param>
        public SubtitlesComponent( ViderePlayer player ) : base( player )
        {

        }

        /// <summary>
        /// Creates the subtitles timer and adds a hook to the <see cref="StateComponent.OnStateChanged"/> event.
        /// </summary>
        protected override void OnInitialize( )
        {
            subtitlesTimer = new DispatcherTimer( );
            subtitlesTimer.Tick += SubtitlesTimerOnTick;

            Player.GetComponent<StateComponent>( ).OnStateChanged += StateHandlerOnOnStateChanged;
        }

        private void StateHandlerOnOnStateChanged( object Sender, OnStateChangedEventArgs StateChangedEventArgs )
        {
            switch ( StateChangedEventArgs.State )
            {
                case StateComponent.PlayerState.Paused:
                    this.subtitlesTimer.Stop( );
                    break;

                case StateComponent.PlayerState.Playing:
                    this.CheckForSubtitles( );
                    break;

                case StateComponent.PlayerState.Stopped:
                    this.StopSubtitles( );
                    break;
            }
        }

        internal void SubtitlesTimerOnTick( object Sender, System.EventArgs Args )
        {
            CheckForSubtitles( );
        }

        private void StopSubtitles( )
        {
            this.OnSubtitlesChanged?.Invoke( this, new OnSubtitlesChangedEventArgs( SubtitleData.Empty ) );
            this.subtitlesTimer.Stop( );
        }

        /// <summary>
        /// The subtitles offset with which it is played.
        /// </summary>
        /// <param name="offset">The offset.</param>
        public void SetSubtitlesOffset( TimeSpan offset )
        {
            subtitlesOffset = offset;
        }

        /// <summary>
        /// Loads a subtitles file.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        public void LoadSubtitles( string filePath )
        {
            if ( !Player.GetComponent<MediaComponent>( ).HasMediaBeenLoaded )
                throw new Exception( "Unable to load subtitles before any media has been loaded." );

            Subtitles = new Subtitles( filePath );
            this.CheckForSubtitles( );
        }

        internal void CheckForSubtitles( )
        {
            if ( !HaveSubtitlesBeenLoaded )
                return;

            TimeSpan position = Player.windowData.MediaPlayer.Position;
            TimeSpan currentPosition = position + subtitlesOffset;
            if ( !Subtitles.AnySubtitlesLeft( currentPosition ) )
                return;

            SubtitleData nextSubs = Subtitles.GetData( currentPosition );
            SubtitleData subs = nextSubs != null ? Subtitles.GetData( nextSubs.Index - 1 ) : Subtitles.GetData( Subtitles.Count - 1 );

            // If we're in the sub interval.
            if ( currentPosition < subs.To && currentPosition >= subs.From )
            {
                this.OnSubtitlesChanged?.Invoke( this, new OnSubtitlesChangedEventArgs( subs ) );

                subtitlesTimer.Interval = subs.To - currentPosition;
            }
            // If we're not in a sub interval
            else
            {
                // Stop the showing of subtitles.
                this.StopSubtitles( );

                // If there are still subs left, change the interval to run when they need to be shown.
                if ( nextSubs != null )
                    subtitlesTimer.Interval = nextSubs.From - currentPosition;
                // Otherwise, don't bother starting the timer again.
                else
                    return;
            }

            subtitlesTimer.Start( );
        }
    }
}
