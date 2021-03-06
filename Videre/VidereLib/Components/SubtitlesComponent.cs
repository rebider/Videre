﻿using System;
using System.IO;
using System.Windows.Threading;
using VidereLib.EventArgs;
using VidereSubs;

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
        /// Gets called whenever subtitles failed to load.
        /// </summary>
        public event EventHandler<OnSubtitlesFailedToLoadEventArgs> OnSubtitlesFailedToLoad;

        /// <summary>
        /// Gets called whenever subtitles have been loaded.
        /// </summary>
        public event EventHandler<OnSubtitlesLoadedEventArgs> OnSubtitlesLoaded;

        /// <summary>
        /// Gets called whenever subtitles have been loaded.
        /// </summary>
        public event EventHandler<OnSubtitlesUnloadedEventArgs> OnSubtitlesUnloaded;

        /// <summary>
        /// The subtitles data currently loaded.
        /// </summary>
        public Subtitles Subtitles { private set; get; }
        private TimeSpan subtitlesOffset;

        private DispatcherTimer subtitlesTimer;

        private bool enabled;

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
            Player.GetComponent<MediaComponent>( ).OnMediaUnloaded += OnOnMediaUnloaded;
            Player.GetComponent<MediaComponent>( ).OnMediaLoaded += OnOnMediaLoaded;
        }

        private void OnOnMediaLoaded( object Sender, OnMediaLoadedEventArgs MediaLoadedEventArgs )
        {
            string mediaName = Path.GetFileNameWithoutExtension( MediaLoadedEventArgs.MediaFile.File.FullName );
            string subtitlesPath = Path.Combine( MediaLoadedEventArgs.MediaFile.File.DirectoryName, mediaName + ".srt" );

            FileInfo subtitlesInfo = new FileInfo( subtitlesPath );
            if ( subtitlesInfo.Exists )
                this.LoadSubtitles( subtitlesInfo.FullName );
        }

        private void OnOnMediaUnloaded( object Sender, OnMediaUnloadedEventArgs MediaUnloadedEventArgs )
        {
            this.UnloadSubtitles( );
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

        /// <summary>
        /// Unloads the currently loaded subtitles.
        /// </summary>
        public void UnloadSubtitles( )
        {
            this.StopSubtitles( );
            this.OnSubtitlesUnloaded?.Invoke( this, new OnSubtitlesUnloadedEventArgs( this.Subtitles ) );
            this.Subtitles = null;
        }

        internal void SubtitlesTimerOnTick( object Sender, System.EventArgs Args )
        {
            CheckForSubtitles( );
        }

        private void StopSubtitles( )
        {
            this.OnSubtitlesChanged?.Invoke( this, new OnSubtitlesChangedEventArgs( SubtitleSegment.Empty ) );
            this.subtitlesTimer.Stop( );
        }

        /// <summary>
        /// The subtitles offset with which it is played.
        /// </summary>
        /// <param name="offset">The offset.</param>
        public void SetSubtitlesOffset( TimeSpan offset )
        {
            subtitlesOffset = offset;
            this.subtitlesTimer.Stop( );

            this.CheckForSubtitles( );
        }

        /// <summary>
        /// Loads a subtitles file.
        /// </summary>
        /// <param name="filePath">The path to the file.</param>
        public void LoadSubtitles( string filePath )
        {
            if ( !Player.GetComponent<MediaComponent>( ).HasMediaBeenLoaded )
                throw new Exception( "Unable to load subtitles before any media has been loaded." );

            if ( HaveSubtitlesBeenLoaded )
                this.UnloadSubtitles( );

            FileInfo file = new FileInfo( filePath );
            Subtitles = Subtitles.LoadSubtitlesFile( filePath );
            enabled = Subtitles.SubtitlesParsedSuccesfully;

            if ( Subtitles.SubtitlesParsedSuccesfully )
            {
                OnSubtitlesLoaded?.Invoke( this, new OnSubtitlesLoadedEventArgs( file ) );
                this.CheckForSubtitles( );
            }
            else
            {
                Subtitles = null;
                OnSubtitlesFailedToLoad?.Invoke( this, new OnSubtitlesFailedToLoadEventArgs( file ) );
            }
        }

        internal void CheckForSubtitles( )
        {
            if ( !HaveSubtitlesBeenLoaded || !enabled )
                return;

            TimeSpan position = Player.GetComponent<TimeComponent>( ).GetPosition( );
            TimeSpan currentPosition = position - subtitlesOffset;
            if ( !Subtitles.AnySubtitlesLeft( currentPosition ) )
                return;

            SubtitleSegment nextSubs = Subtitles.GetData( currentPosition );
            SubtitleSegment subs = nextSubs != null ? Subtitles.GetData( nextSubs.Index - 1 ) : Subtitles.GetData( Subtitles.Count - 1 );

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

        /// <summary>
        /// Enables the subtitles.
        /// </summary>
        public void Enable( )
        {
            enabled = true;
            CheckForSubtitles( );
        }

        /// <summary>
        /// Disables the subtitles.
        /// </summary>
        public void Disable( )
        {
            enabled = false;
            this.StopSubtitles( );
        }
    }
}
