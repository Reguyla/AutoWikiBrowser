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

    public class ObjectCache : IDisposable
    {
        public ObjectCache()
        {
            AddType(typeof(string), DefaultLifespan);
            AddType(typeof(List<string>), DefaultLifespan);
            //AddType(typeof(string[]), DefaultLifespan);
            AddType(typeof(SiteInfo), DefaultLifespan);
            AddType(typeof(bool), DefaultLifespan);
        }

        public ObjectCache(string fileName)
            : this()
        {
            Load(fileName);
        }

        static ObjectCache()
        {
            Global = new ObjectCache(Path.Combine(AwbDirs.UserData, "ObjectCache.xml"));
            Global.AddType(typeof(SiteInfo), DefaultLifespan);
        }


        /// <summary>
        /// Saves the current cache and releases this instance.
        /// </summary>
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
        /// 
        /// </summary>
        public static ObjectCache Global
        { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public string FileName
        { get; private set; }

        private class StoredData
        {
            public readonly object Data;
            public readonly DateTime Expires;

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
        /// 
        /// </summary>
        /// <param name="what"></param>
        /// <param name="lifeSpan"></param>
        public void AddType(Type what, TimeSpan lifeSpan)
        {
            if (what == null) throw new ArgumentNullException("what");

            SupportedTypes[what] = lifeSpan;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
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
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Set(string key, object value)
        {
            Set(key, value, DateTime.MinValue);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="duration"></param>
        public void Set(string key, object value, TimeSpan duration)
        {
            Set(key, value, DateTime.Now + duration);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expiry"></param>
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
        /// 
        /// </summary>
        /// <param name="str"></param>
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