﻿using System;
using System.Net.TMDb;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using VidereLib.Components;
using VidereLib.Data;
using VidereLib.Networking;

namespace VidereLib.NetworkingRequests
{
    /// <summary>
    /// A job for requesting movie info.
    /// </summary>
    public class RequestMovieInfoJob
    {
        private readonly VidereMedia media;
        private readonly ServiceClient client;

        /// <summary>
        /// Called whenever a service request exception is thrown.
        /// </summary>
        public event EventHandler<ServiceRequestException> OnServiceException; 

        /// <summary>
        /// Called whenever the request are suspended due to too many requests having been sent.
        /// </summary>
        public event EventHandler OnRequestLimitReached; 

        private readonly CancellationTokenSource token;

        private TheMovieDBRequest<Resource> idRequest;
        private TheMovieDBRequest<Movie> movieRequest;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="media">The media to find information for.</param>
        public RequestMovieInfoJob( VidereMedia media )
        {
            client = new ServiceClient( TheMovieDBComponent.APIKey );
            this.media = media;
            this.token = new CancellationTokenSource( );
        }

        /// <summary>
        /// Cancels the request.
        /// </summary>
        public void Cancel( )
        {
            token.Cancel( );
            idRequest?.Cancel( );
            movieRequest?.Cancel( );
        }

        /// <summary>
        /// Initiates the request.
        /// </summary>
        public async Task<Movie> Request( )
        {
            if ( media.IMDBID == null )
                return null;

            idRequest = new TheMovieDBRequest<Resource>( async ( ) => await client.FindAsync( "tt" + media.IMDBID, "imdb_id", token.Token ) );
            idRequest.OnRequestLimitReached += ( Sender, Args ) => this.OnRequestLimitReached?.Invoke( this, null );
            Resource Res = await idRequest.Request( );
            if ( Res == null )
                return null;

            movieRequest = new TheMovieDBRequest<Movie>( async ( ) => await client.Movies.GetAsync( Res.Id, "en", false, token.Token ) );
            movieRequest.OnRequestLimitReached += ( Sender, Args ) => this.OnRequestLimitReached?.Invoke( this, null );
            Movie movie = await movieRequest.Request( );
            return movie;
        }
    }
}
