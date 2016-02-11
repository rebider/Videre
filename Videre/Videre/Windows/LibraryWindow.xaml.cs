﻿using System;
using System.Windows.Controls;
using VidereLib.Data;

namespace Videre.Windows
{
    /// <summary>
    /// Interaction logic for LibraryWindow.xaml
    /// </summary>
    public partial class LibraryWindow
    {
        /// <summary>
        /// The selected media.
        /// </summary>
        public VidereMedia Media { private set; get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public LibraryWindow( )
        {
            InitializeComponent( );
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.FrameworkElement.Initialized"/> event. This method is invoked whenever <see cref="P:System.Windows.FrameworkElement.IsInitialized"/> is set to true internally. 
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.RoutedEventArgs"/> that contains the event data.</param>
        protected override void OnInitialized( EventArgs e )
        {
            base.OnInitialized( e );

            this.DirectorySelector.DirectoryList.SelectionChanged += DirectoryListOnSelectionChanged;
            this.DirectorySelector.SetDirectories( Settings.Default.MediaFolders.ToArray( ) );
        }

        private void DirectoryListOnSelectionChanged( object Sender, SelectionChangedEventArgs SelectionChangedEventArgs )
        {
            if ( SelectionChangedEventArgs.AddedItems.Count <= 0 ) return;

            foreach ( object dir in SelectionChangedEventArgs.AddedItems )
                this.MediaShowcase.LoadDirectory( dir as string );
        }
    }
}