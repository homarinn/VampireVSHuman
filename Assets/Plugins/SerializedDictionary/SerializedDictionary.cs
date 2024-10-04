using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SerializedDictionary<K, V> : Dictionary<K, V>, ISerializationCallbackReceiver
{
    [Serializable]
    internal class KeyValue
    {
        public K Key;
        public V Value;

        public KeyValue(K key, V value)
        {
            Key = key;
            Value = value;
        }
    }

    [SerializeField]
    private List<KeyValue> itemList = new List<KeyValue>();

    void ISerializationCallbackReceiver.OnAfterDeserialize()
    {
        base.Clear();
        foreach (var item in itemList)
        {
            if (ContainsKey(item.Key))
            {
                continue;
            }
            this[item.Key] = item.Value;
        }
    }

    void ISerializationCallbackReceiver.OnBeforeSerialize()
    {
    }

    public new void Add(K key, V value)
    {
        base.Add(key, value);
        itemList.Add(new KeyValue(key, value));
    }

    public new void Clear()
    {
        base.Clear();
        itemList.Clear();
    }

    public new V this[K key]
    {
        get => base[key];
        set
        {
            base[key] = value;
            var item = itemList.Find(kv => kv.Key.Equals(key));
            if (item != null)
            {
                item.Value = value;
            }
            else
            {
                itemList.Add(new KeyValue(key, value));
            }
        }
    }

    public new bool Remove(K key)
    {
        bool removed = base.Remove(key);
        if (removed)
        {
            itemList.RemoveAll(kv => kv.Key.Equals(key));
        }
        return removed;
    }

    public virtual K DefaultKey => default;
}
