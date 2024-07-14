using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Exiled.API.Features;
using MEC;
using Random = UnityEngine.Random;

namespace TextDB;

public class TextDatabase : IReadOnlyDictionary<string, string>
{
    string RootDirectory { get; } = $"{Paths.Plugins}/TextDB/";

    static readonly Dictionary<string, TextDatabase> Databases = [];

    public static float SynchronizationInterval { get; set; } = 15f;

    public static TextDatabase Open(string name)
        => Databases.GetValueOrDefault(name) ?? (Databases[name] = new TextDatabase(name));

    TextDatabase(string name)
    {
        char[] forbiddenChars = name.Intersect(Path.GetInvalidFileNameChars()).ToArray();
        if (forbiddenChars.Length > 0)
            throw new ArgumentException("Name cannot contain the following characters: " + string.Join(", ", forbiddenChars));

        _name = name;
        _path = Path.Combine(RootDirectory, _name + ".txt");

        Directory.CreateDirectory(RootDirectory);

        if (!File.Exists(_path))
            File.WriteAllText(_path, "");

        _dictionary = new Dictionary<string, string>();
        _updateCoroutine = Timing.RunCoroutine(UpdateCoroutine());
    }

    ~TextDatabase()
    {
        Timing.KillCoroutines(_updateCoroutine);
        Sync();
    }

    readonly CoroutineHandle _updateCoroutine;
    public const char KeySeparator = 'Ǽ';
    readonly string _name;
    readonly string _path;
    readonly Dictionary<string, string> _dictionary;
    readonly ConcurrentQueue<Action<Dictionary<string, string>>> _edits = new();

    //Read

    public int Count => _dictionary.Count;
    public IEnumerable<string> Keys => _dictionary.Keys;
    public IEnumerable<string> Values => _dictionary.Values;

    public bool ContainsKey(string key) => _dictionary.ContainsKey(key);

    public bool TryGetValue(string key, out string value) => _dictionary.TryGetValue(key, out value);

    //Write

    public string this[string key]
    {
        get => _dictionary[key];
        set
        {
            Validate(key, "Key");
            Validate(value, "Value");
            Edit(d => d[key] = value);
        }
    }

    public void Add(string key, string value)
    {
        Validate(key, "Key");
        Validate(value, "Value");
        Edit(d => d.Add(key, value));
    }

    public void Remove(string key) => Edit(d => d.Remove(key));

    public void Clear() => Edit(d => d.Clear());

    //Internal

    void Edit(Action<Dictionary<string, string>> edit)
    {
        edit(_dictionary);
        _edits.Enqueue(edit);
    }

    static void Validate(string item, string paramName)
    {
        if (item.Contains(KeySeparator))
            throw new ArgumentException($"The {paramName} cannot contain the key separator character '{KeySeparator}'.");
    }

    IEnumerator<float> UpdateCoroutine()
    {
        while (true)
        {
            try
            {
                Sync();
            }
            catch (IOException)
            {
                Log.Debug($"TextDB \"{_name}\" synchronization: File already in use, retrying in {SynchronizationInterval + Random.Range(3f, 6f):F2} seconds.");
            }
            catch (Exception e)
            {
                Log.Error($"Unexpected error in TextDB \"{_name}\" synchronization: {e}");
            }

            yield return Timing.WaitForSeconds(SynchronizationInterval);
        }
    }

    void Sync()
    {
        if (File.ReadAllText(_path) is { Length: > 0 } text)
        {
            string[] items = text.Split(KeySeparator);

            if (items.Length % 2 != 0)
                throw new InvalidOperationException("Text database is corrupted - odd number of items.");

            _dictionary.Clear();
            for (var i = 0; i < items.Length; i += 2)
            {
                string key = items[i];
                string value = items[i + 1];
                _dictionary.Add(key, value);
            }
        }

        if (_edits.Count != 0)
        {
            while (_edits.TryDequeue(out Action<Dictionary<string, string>> edit))
            {
                try
                {
                    edit(_dictionary);
                }
                catch (Exception e)
                {
                    Log.Error($"Error while applying TextDB edit: {e}");
                }
            }

            text = string.Join(KeySeparator, _dictionary.Select(pair => pair.Key + KeySeparator + pair.Value));
            File.WriteAllText(_path, text);
        }
    }

    // Interfaces

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => throw new NotImplementedException();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}