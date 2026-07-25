/*
WikiFunctions
Copyright (C) 2009 Max Semenik

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

/* 
 * This file contains classes to be used for persisting different kinds of crap we currently load from network.
 */

using System.Diagnostics;
using System.Xml.Serialization;

namespace WikiFunctions
{
    /// <summary>
    /// Stores cached objects grouped by type and persists them between sessions
    /// when backed by a cache file.
    /// </summary>
    /// <remarks>
    /// The cache is initialized with a default set of commonly cached types.
    /// Additional types can be registered by calling <see cref="AddType(Type, TimeSpan)"/>.
    /// </remarks>
    public class ObjectCache : IDisposable
    {
        /// <summary>
        /// Initializes a new in-memory object cache using the default cached types.
        /// </summary>
        public ObjectCache()
        {
            AddType(typeof(string), DefaultLifespan);
            AddType(typeof(List<string>), DefaultLifespan);
            AddType(typeof(SiteInfo), DefaultLifespan);
            AddType(typeof(bool), DefaultLifespan);
        }

        /// <summary>
        /// Initializes a new object cache and loads its contents from the specified file.
        /// </summary>
        /// <param name="fileName">
        /// The cache file to load.
        /// </param>
        /// <remarks>
        /// If the cache file does not exist or cannot be loaded, the cache remains
        /// initialized with the default registered types.
        /// </remarks>
        public ObjectCache(string fileName)
            : this()
        {
            Load(fileName);
        }

        /// <summary>
        /// Initializes the shared application-wide object cache.
        /// </summary>
        /// <remarks>
        /// This constructor is called automatically before the cache is first used.
        /// The global cache is backed by the user's ObjectCache.xml file.
        /// </remarks>
        static ObjectCache()
        {
            Global = new ObjectCache(Path.Combine(AwbDirs.UserData, "ObjectCache.xml"));
            Global.AddType(typeof(SiteInfo), DefaultLifespan);
        }

        /// <summary>
        /// Saves the cache to its configured backing file and releases this instance.
        /// </summary>
        /// <remarks>
        /// Disposal is safe to call multiple times. Any exceptions that occur while
        /// saving the cache are reported for diagnostics and are not propagated to
        /// the caller.
        /// </remarks>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                if (!string.IsNullOrEmpty(FileName))
                    Save();
            }
            catch (Exception ex)
            {
                ReportException(ex);
            }
            finally
            {
                FileName = null;
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Gets the shared application-wide object cache.
        /// </summary>
        /// <remarks>
        /// The global cache is initialized from the cache file stored in the
        /// application's user-data directory.
        /// </remarks>
        public static ObjectCache Global
        { get; private set; }

        /// <summary>
        /// Gets the path of the file used to load and persist this cache.
        /// </summary>
        /// <value>
        /// The backing cache-file path, or <see langword="null"/> when the cache is
        /// not associated with a file.
        /// </value>
        public string FileName
        { get; private set; }

        /// <summary>
        /// Represents a cached value and the time at which it expires.
        /// </summary>
        private class StoredData
        {
            public readonly object Data;
            public readonly DateTime Expires;

            /// <summary>
            /// Initializes a cached value with its expiration time.
            /// </summary>
            /// <param name="data">The value being cached.</param>
            /// <param name="expires">The date and time at which the value expires.</param>
            public StoredData(object data, DateTime expires)
            {
                Data = data;
                Expires = expires;
            }
        }

        private static readonly TimeSpan DefaultLifespan = new TimeSpan(5, 0, 0, 0);
        private readonly Dictionary<Type, TimeSpan> SupportedTypes = new Dictionary<Type, TimeSpan>();

        private readonly Dictionary<Type, Dictionary<string, StoredData>> Storage
            = new Dictionary<Type, Dictionary<string, StoredData>>();

        /// <summary>
        /// Registers a type that may be stored in the cache and specifies its
        /// default lifespan.
        /// </summary>
        /// <param name="what">
        /// The type to register.
        /// </param>
        /// <param name="lifeSpan">
        /// The amount of time values of the registered type remain valid.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="what"/> is <see langword="null"/>.
        /// </exception>
        public void AddType(Type what, TimeSpan lifeSpan)
        {
            if (what == null) throw new ArgumentNullException("what");

            SupportedTypes[what] = lifeSpan;
        }

        /// <summary>
        /// Gets or sets an object associated with the specified cache key.
        /// </summary>
        /// <param name="key">
        /// The key identifying the cached object.
        /// </param>
        /// <returns>
        /// The cached object when found and not expired; otherwise,
        /// <see langword="null"/>.
        /// </returns>
        /// <remarks>
        /// Values accessed through this indexer are stored and retrieved using the
        /// <see cref="object"/> cache registration. Use the generic cache methods
        /// when the value should be associated with a more specific type.
        /// </remarks>
        public object this[string key]
        {
            set
            {
                Set(key, value);
            }
            get
            {
                return Get<object>(key);
            }
        }

        /// <summary>
        /// Retrieves a cached value associated with the specified key.
        /// </summary>
        /// <typeparam name="T">
        /// The expected type of the cached value.
        /// </typeparam>
        /// <param name="key">
        /// The key identifying the cached value.
        /// </param>
        /// <returns>
        /// The cached value when present and not expired; otherwise, <see langword="null"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="key"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="InvalidCastException">
        /// Thrown when the stored value cannot be cast to <typeparamref name="T"/>.
        /// </exception>
        // TODO: Review callers and consider returning T instead of object.
        // The method already casts stored values to T, so a generic return type
        // would provide stronger type safety. Define the intended behavior for
        // missing or expired value types before making this API change.
        public object Get<T>(string key)
        {
            ArgumentNullException.ThrowIfNull(key);

            lock (Storage)
            {
                Type type = typeof(T);

                if (!Storage.TryGetValue(
                        type,
                        out Dictionary<string, StoredData> values)
                    || !values.TryGetValue(
                        key,
                        out StoredData storedData))
                {
                    return null;
                }

                if (storedData.Expires < DateTime.Now)
                {
                    values.Remove(key);
                    return null;
                }

                return (T)storedData.Data;
            }
        }

        /// <summary>
        /// Stores a value in the cache using the default lifespan registered for
        /// the value's type.
        /// </summary>
        /// <param name="key">
        /// The key used to identify the cached value.
        /// </param>
        /// <param name="value">
        /// The value to store.
        /// </param>
        /// <remarks>
        /// The actual expiration time is determined by the overload that accepts an
        /// absolute expiration date.
        /// </remarks>
        public void Set(string key, object value)
        {
            Set(key, value, DateTime.MinValue);
        }

        /// <summary>
        /// Stores a value in the cache for the specified duration.
        /// </summary>
        /// <param name="key">
        /// The key used to identify the cached value.
        /// </param>
        /// <param name="value">
        /// The value to store.
        /// </param>
        /// <param name="duration">
        /// The amount of time the cached value remains valid.
        /// </param>
        public void Set(string key, object value, TimeSpan duration)
        {
            Set(key, value, DateTime.Now + duration);
        }

        /// <summary>
        /// Stores a value in the cache until the specified expiration time.
        /// </summary>
        /// <param name="key">
        /// The key used to identify the cached value.
        /// </param>
        /// <param name="value">
        /// The value to store. Its runtime type must be registered with
        /// <see cref="AddType(Type, TimeSpan)"/>.
        /// </param>
        /// <param name="expiry">
        /// The date and time at which the value expires. Specify
        /// <see cref="DateTime.MinValue"/> to use the default lifespan registered
        /// for the value's type.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="value"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="key"/> is null or empty, or when the value's
        /// runtime type is not supported by the cache.
        /// </exception>
        public void Set(string key, object value, DateTime expiry)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException("key");
            if (value == null) throw new ArgumentNullException("value");

            Type type = value.GetType();
            if (!SupportedTypes.ContainsKey(type))
                throw new ArgumentException("Caching of type " + value.GetType().Name + " is not supported",
                                            "value");

            if (expiry == DateTime.MinValue) expiry = DateTime.Now + SupportedTypes[type];

            lock (Storage)
            {
                if (!Storage.ContainsKey(type))
                    Storage[type] = new Dictionary<string, StoredData>();
                Storage[type][key] = new StoredData(value, expiry);
            }
        }

        private XmlSerializer serializer;
        private bool _disposed;
        private XmlSerializer Serializer
        {
            get
            {
                if (serializer != null) return serializer;

                var usedTypes = new List<Type>();
                foreach (var type in SupportedTypes)
                {
                    usedTypes.Add(type.Key);
                }
                serializer = new XmlSerializer(typeof(Internal.CacheRoot), usedTypes.ToArray());
                return serializer;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void Save()
        {
            Save(FileName);
        }

        /// <summary>
        /// Saves the cache to the specified file using a temporary file so an
        /// interrupted write does not corrupt the existing cache.
        /// </summary>
        /// <param name="fileName">The cache file to write.</param>
        public void Save(string fileName)
        {
            ArgumentException.ThrowIfNullOrEmpty(fileName);

            string? directory = Path.GetDirectoryName(fileName);

            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            string temporaryFileName = fileName + ".tmp";

            try
            {
                using (FileStream stream = new(
                           temporaryFileName,
                           FileMode.Create,
                           FileAccess.Write,
                           FileShare.None))
                {
                    Save(stream);
                }

                File.Move(
                    temporaryFileName,
                    fileName,
                    overwrite: true);

                FileName = fileName;
            }
            finally
            {
                if (File.Exists(temporaryFileName))
                    File.Delete(temporaryFileName);
            }
        }

        /// <summary>
        /// Loads ObjectCache.xml if it exists. Invalid cache files are deleted after
        /// the input stream has been closed.
        /// </summary>
        /// <param name="fileName">The cache file to load.</param>
        public void Load(string fileName)
        {
            ArgumentException.ThrowIfNullOrEmpty(fileName);

            FileName = fileName;

            if (!File.Exists(fileName))
                return;

            try
            {
                bool loadedSuccessfully;

                using (FileStream fs = new(
                           fileName,
                           FileMode.Open,
                           FileAccess.Read,
                           FileShare.Read))
                {
                    loadedSuccessfully = Load(fs);
                }

                if (!loadedSuccessfully && File.Exists(fileName))
                    File.Delete(fileName);
            }
            catch (Exception ex)
            {
                ReportException(ex);
            }
        }

        /// <summary>
        /// Serializes the current cache contents to the specified stream.
        /// </summary>
        /// <param name="str">
        /// The destination stream that receives the serialized cache.
        /// </param>
        /// <remarks>
        /// Expired cache entries are omitted during serialization.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="str"/> is <see langword="null"/>.
        /// </exception>
        public void Save(Stream str)
        {
            var root = new Internal.CacheRoot();

            DateTime now = DateTime.Now;

            lock (Storage)
            {
                foreach (var type in Storage)
                {
                    var typeRoot = new Internal.Type { Name = type.Key.ToString() };
                    foreach (var value in type.Value)
                    {
                        if (value.Value.Expires < now) continue;
                        typeRoot.Items.Add(new Internal.Item { Value = value.Value.Data, Expires = value.Value.Expires, Key = value.Key });
                    }
                    if (typeRoot.Items.Count > 0) root.Types.Add(typeRoot);
                }
            }

            Serializer.Serialize(str, root);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ex"></param>
        private void ReportException(Exception ex)
        {
            Trace.WriteLine("Exception caught in ObjectCache: " + ex.Message);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public bool Load(Stream str)
        {
            if (str == null) throw new ArgumentNullException("str");

            try
            {
                var loaded = (Internal.CacheRoot)Serializer.Deserialize(str);
                if (loaded.Version != Globals.WikiFunctionsVersion.ToString()) return false;

                lock (Storage)
                {
                    Storage.Clear();

                    foreach (var entry in loaded.Types)
                    {
                        try
                        {
                            Type type = Type.GetType(entry.Name);
                            if (type == null || !SupportedTypes.ContainsKey(type)) continue;

                            Storage[type] = new Dictionary<string, StoredData>();
                            foreach (var data in entry.Items)
                                Storage[type][data.Key] = new StoredData(data.Value, data.Expires);
                        }
                        catch (Exception ex)
                        {
                            // Ignore possible exceptions, attempting
                            ReportException(ex);
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                ReportException(ex);
                return false;
            }
        }

        public void Invalidate()
        {
            lock (Storage)
            {
                Storage.Clear();
            }
            Save();
            File.Delete(FileName);
        }
    }

    namespace Internal
    {
        [Serializable]
        public class Item
        {
            [XmlAttribute("key")]
            public string Key;

            [XmlAttribute("expires")]
            public DateTime Expires;

            //[XmlText]
            public object Value;
        }

        [Serializable/*, XmlElement("Type")*/]
        public class Type
        {
            [XmlAttribute("name")]
            public string Name;

            public readonly List<Item> Items = new List<Item>();
        }

        [Serializable, XmlRoot("Cache")]
        public class CacheRoot
        {
            public CacheRoot()
            { }

            public CacheRoot(string version)
            {
                Version = version;
            }

            [XmlAttribute("version")]
            public readonly string Version = Globals.WikiFunctionsVersion.ToString();

            // [XmlText]
            [XmlArray("Types")]
            public readonly List<Type> Types = new List<Type>();
        }
    }
}