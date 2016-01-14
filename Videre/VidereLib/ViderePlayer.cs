﻿using System;
using System.Collections.Generic;
using System.Reflection;
using VidereLib.Components;

namespace VidereLib
{
    public class ViderePlayer
    {
        internal readonly WindowData windowData;

        private readonly Dictionary<Type, ComponentBase> components = new Dictionary<Type, ComponentBase>( );

        /// <summary>
        /// Constructor.
        /// </summary>
        public ViderePlayer( WindowData data )
        {
            windowData = data;

            this.LoadComponents( );
            this.InitializeComponents( );
        }

        private void LoadComponents( )
        {
            Assembly A = Assembly.GetExecutingAssembly( );

            foreach ( Type T in A.GetTypes( ) )
                if ( T.IsSubclassOf( typeof ( ComponentBase ) ) )
                    components.Add( T, ( ComponentBase ) Activator.CreateInstance( T, this ) );
        }

        private void InitializeComponents( )
        {
            foreach ( KeyValuePair<Type, ComponentBase> pair in components )
                pair.Value.Initialize( );
        }

        /// <summary>
        /// Gets the <typeparamref name="T"/> in the <see cref="ViderePlayer"/>.
        /// </summary>
        /// <typeparam name="T">The type of the <see cref="ComponentBase"/>.</typeparam>
        /// <returns>The component.</returns>
        public T GetComponent<T>( ) where T : ComponentBase
        {
            return components[ typeof ( T ) ] as T;
        }
    }
}