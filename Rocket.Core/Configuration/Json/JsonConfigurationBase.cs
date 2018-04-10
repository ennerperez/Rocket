﻿using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Rocket.API.Configuration;

namespace Rocket.Core.Configuration.Json
{
    public class JsonConfigurationBase : IConfigurationBase
    {
        public JToken Node { get; protected set; }

        public JsonConfigurationBase(JToken node)
        {
            Node = node ?? throw new ArgumentNullException(nameof(node));
        }

        protected JsonConfigurationBase()
        {

        }

        public IConfigurationSection this[string path] => GetSection(path);

        public IConfigurationSection GetSection(string path)
        {
            GuardLoaded();
            GuardPath(path);

            JsonConfigurationBase currentNode = this;
            string[] parts = path.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 1)
            {
                string key = parts[0];
                if (Node is JObject o && !o.ContainsKey(key))
                {
                    o.Add(new JObject(key, null));
                }

                return new JsonConfigurationSection(Node[key], key);
            }

            foreach (string part in parts) currentNode = (JsonConfigurationSection)currentNode.GetSection(part);

            return (IConfigurationSection)currentNode;
        }

        public IConfigurationSection CreateSection(string path, bool isValue)
        {
            GuardLoaded();
            GuardPath(path);

            JToken current = Node;

            string[] parts = path.Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);

            int i = 0;
            foreach (string part in parts)
            {
                if (!(current is JObject o) || o.ContainsKey(part))
                {
                    current = current[part];
                    i++;
                    continue;
                }

                if (i == (parts.Length - 1) && isValue)
                {
                    o.Add(new JProperty(part, ""));
                }
                else
                {
                    o.Add(part, new JObject());
                }

                current = current[part];
                i++;
            }

            return GetSection(path);
        }

        public bool RemoveSection(string path)
        {
            GuardLoaded();
            GuardPath(path);
            ((JsonConfigurationSection)GetSection(path)).Node.Parent.Remove();
            return true;
        }

        public IEnumerable<IConfigurationSection> GetChildren()
        {
            GuardLoaded();

            List<IConfigurationSection> sections = new List<IConfigurationSection>();
            foreach (JToken node in Node.Children())
            {
                string childPath = node.Path.Replace(Node.Path + ".", "");
                sections.Add(GetSection(childPath));
            }

            return sections;
        }

        public void GuardLoaded()
        {
            if (Node == null)
                throw new ConfigurationNotLoadedException();
        }

        public void GuardPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Configuration paths can not be null or empty");
        }

        public virtual T Get<T>() => Node.ToObject<T>();
        public object Get(Type t) => Node.ToObject(t);

        public T Get<T>(T defaultValue)
        {
            GuardLoaded();

            if (!TryGet(out T val)) val = defaultValue;

            return val;
        }

        public object Get(Type t, object defaultValue)
        {
            GuardLoaded();

            if (!TryGet(t, out object val)) val = defaultValue;

            return val;
        }

        public virtual void Set(object o)
        {
            GuardLoaded();
            var node = Node;

            if (node is JValue)
                node = node.Parent;

            if (!(node is JProperty p))
                throw new Exception("Can not set value of non-property: " + Node.Path);

            p.Value = new JValue(o);
        }

        public bool TryGet<T>(out T value)
        {
            GuardLoaded();

            value = default(T);
            try
            {
                value = Get<T>();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool TryGet(Type t, out object value)
        {
            GuardLoaded();

            value = null;
            try
            {
                value = Get(t);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public IEnumerator<IConfigurationSection> GetEnumerator() => GetChildren().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}