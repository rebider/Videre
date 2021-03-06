﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VidereSubs.Attributes;

namespace VidereSubs.SubtitleFormats
{
    /// <summary>
    /// The .SRT file format.
    /// </summary>
    [SubtitleLoader( "srt" )]
    public class SRT : Subtitles
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="FilePath">The path to the .srt file.</param>
        public SRT( string FilePath ) : base( FilePath )
        {
        }

        /// <summary>
        /// Parses the file and returns a dictionary containing the timespan at which it starts as the key, and the actual subtitle data as the value.
        /// </summary>
        /// <param name="FilePath">The path to the subtitle file.</param>
        /// <returns>The subtitle data.</returns>
        protected override Dictionary<TimeSpan, SubtitleSegment> ParseFile( string FilePath )
        {
            if ( !File.Exists( FilePath ) )
                return null;

            Dictionary<TimeSpan, SubtitleSegment> subtitles = new Dictionary<TimeSpan, SubtitleSegment>( );
            CultureInfo france = new CultureInfo( "fr-Fr" );
            string[ ] Data;
            using ( FileStream FS = File.OpenRead( FilePath ) )
                using ( TextReader Reader = new StreamReader( FS ) )
                    Data = Reader.ReadToEnd( ).Split( new[ ] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries );

            int X = 0;
            while ( X < Data.Length - 1 )
            {
                int ID = int.Parse( Data[ X++ ] );

                string[ ] SplitTime = Data[ X++ ].Split( new[ ] { "-->" }, StringSplitOptions.RemoveEmptyEntries );
                TimeSpan Start = TimeSpan.Parse( SplitTime[ 0 ].Trim( ), france );
                TimeSpan End = TimeSpan.Parse( SplitTime[ 1 ].Trim( ), france );

                List<string> subs = new List<string>( );
                int parsed;

                while ( Data[ X ].Length > 0 && X < Data.Length - 1 && !int.TryParse( Data[ X ], out parsed ) && parsed != ID + 1 )
                {
                    subs.Add( Data[ X ] );
                    X++;
                }

                subtitles.Add( Start, new SubtitleSegment( ID, Start, End, subs ) );
            }

            return subtitles;
        }
    }
}