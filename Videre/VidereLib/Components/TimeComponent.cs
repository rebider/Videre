﻿using System;
using System.Windows.Threading;
using VidereLib.EventArgs;

namespace VidereLib.Components
{
    /// <summary>
    /// The time component.
    /// </summary>
    public class TimeComponent : ComponentBase
    {
        private bool pausedWhenChangingPosition;

        private DispatcherTimer timeTimer;

        /// <summary>
        /// Gets called whenever the position in the media file has changed.
        /// </summary>
        public event EventHandler<OnPositionChangedEventArgs> OnPositionChanged;
        private TimeSpan previousTimeSpan;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="player">The <see cref="ViderePlayer"/>.</param>
        public TimeComponent( ViderePlayer player ) : base( player )
        {
            player.GetComponent<StateComponent>(  ).OnStateChanged += OnOnStateChanged;
        }

        private void OnOnStateChanged( object Sender, OnStateChangedEventArgs StateChangedEventArgs )
        {
            switch ( StateChangedEventArgs.State )
            {
                case StateComponent.PlayerState.Paused:
                case StateComponent.PlayerState.Stopped:
                    timeTimer.Stop( );
                    break;

                case StateComponent.PlayerState.Playing:
                    timeTimer.Start( );
                    break;
            }
        }

        /// <summary>
        /// Creates the timing timer.
        /// </summary>
        protected override void OnInitialize( )
        {
            timeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds( 100 ) };
            timeTimer.Tick += TimeTimerOnTick;
        }

        /// <summary>
        /// Gets the current position in the media as a <see cref="TimeSpan"/>.
        /// </summary>
        /// <returns>The current position in the media.</returns>
        public TimeSpan GetPosition( )
        {
            return Player.windowData.MediaPlayer.Position;
        }

        private void TimeTimerOnTick( object Sender, System.EventArgs Args )
        {
            if ( !Player.GetComponent<MediaComponent>( ).HasMediaBeenLoaded )
                return;

            if ( GetPosition(  ) == previousTimeSpan ) return;

            double progress = Player.windowData.MediaPlayer.Position.TotalSeconds / Player.windowData.MediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
            OnPositionChanged?.Invoke( this, new OnPositionChangedEventArgs( GetPosition(  ), progress ) );
            previousTimeSpan = GetPosition( );
        }

        /// <summary>
        /// Sets the position in the media to play at.
        /// </summary>
        /// <param name="Progress">The float containing the progress of the media, 0 being the start and 1 being the end.</param>
        public void SetPosition( double Progress )
        {
            if ( !Player.GetComponent<MediaComponent>( ).HasMediaBeenLoaded )
                throw new Exception( "No media loaded." );

            if ( Progress < 0 )
                Progress = 0;

            if ( Progress > 1 )
                Progress = 1;

            TimeSpan duration = Player.GetComponent<MediaComponent>( ).GetMediaLength( );
            TimeSpan newPositon = new TimeSpan( ( long )( duration.Ticks * Progress ) );

            SetPosition( newPositon );
            this.Player.GetComponent<SubtitlesComponent>( ).CheckForSubtitles( ); 
        }

        /// <summary>
        /// Sets the position in the media to play at.
        /// </summary>
        /// <param name="Span">The <see cref="TimeSpan"/> to set the player to.</param>
        public void SetPosition( TimeSpan Span )
        {
            if ( !Player.GetComponent<MediaComponent>( ).HasMediaBeenLoaded )
                throw new Exception( "No media loaded." );


            Player.windowData.MediaPlayer.Position = Span;
        }

        /// <summary>
        /// Starts the changing of the position.
        /// </summary>
        public void StartChangingPosition( )
        {
            StateComponent stateHandler = Player.GetComponent<StateComponent>( );
            pausedWhenChangingPosition = stateHandler.CurrentState == StateComponent.PlayerState.Paused;
            stateHandler.Pause( );
            stateHandler.CurrentState = StateComponent.PlayerState.ChangingPosition;
        }

        /// <summary>
        /// Stops the changing of the position.
        /// </summary>
        public void StopChangingPosition( )
        {
            StateComponent stateHandler = Player.GetComponent<StateComponent>( );

            if ( stateHandler.CurrentState != StateComponent.PlayerState.ChangingPosition )
                throw new Exception( "Can't stop changing the position if not currently changing the position." );

            if ( !pausedWhenChangingPosition )
                stateHandler.Play( );
            else
                stateHandler.CurrentState = StateComponent.PlayerState.Paused;
        }

    }
}
