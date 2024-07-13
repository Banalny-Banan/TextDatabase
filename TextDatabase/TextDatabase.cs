using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Exiled.API.Features;
using MEC;

namespace TextDatabase;

public class TextDatabase : IReadOnlyDictionary<string, string>
{
    string RootDirectory { get; } = $"{Paths.Plugins}/TextDatabase/";

    static readonly Dictionary<string, TextDatabase> Databases = [];

    public static TextDatabase Open(string name, string keySeparator = "\n")
        => Databases.GetValueOrDefault(name) ?? (Databases[name] = new TextDatabase(name, keySeparator));

    TextDatabase(string name, string keySeparator = "\n")
    {
        char[] forbiddenChars = name.Intersect(Path.GetInvalidFileNameChars()).ToArray();
        if (forbiddenChars.Length > 0)
            throw new ArgumentException("Name cannot contain the following characters: " + string.Join(", ", forbiddenChars));

        _name = name;
        _keySeparator = keySeparator;
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
        Flush();
    }

    readonly CoroutineHandle _updateCoroutine;
    readonly string _keySeparator;
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

    void Validate(string item, string paramName)
    {
        if (item.Contains(_keySeparator))
            throw new ArgumentException($"The {paramName} cannot contain the key separator character.");
    }
        
    IEnumerator<float> UpdateCoroutine()
    {
        while (true)
        {
            try
            {
                Flush();
            }
            catch (IOException)
            {
                Log.Debug($"TextDatabase \"{_name}\" flush: File already in use, retrying in 15 seconds.");
            }
            catch (Exception e)
            {
                Log.Error($"Unexpected error in TextDatabase \"{_name}\" flush: {e}");
            }

            yield return Timing.WaitForSeconds(15f);
        }
    }

    void Flush()
    {
        string text = File.ReadAllText(_path);
        string[] items = text.Split(_keySeparator);
        _dictionary.Clear();

        if (items.Length % 2 != 0)
            throw new InvalidOperationException("Text database is corrupted - odd number of items.");

        for (var i = 0; i < items.Length; i += 2)
        {
            string key = items[i];
            string value = items[i + 1];
            _dictionary.Add(key, value);
        }

        if (_edits.Count == 0)
            return;

        while (_edits.TryDequeue(out Action<Dictionary<string, string>> edit))
        {
            try
            {
                edit(_dictionary);
            }
            catch (Exception e)
            {
                Log.Error($"Error while applying TextDatabase edit: {e}");
                throw;
            }
        }

        text = string.Join(_keySeparator, _dictionary.Select(pair => pair.Key + _keySeparator + pair.Value));
        File.WriteAllText(_path, text);
    }
    
    // Interfaces
    
    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => throw new NotImplementedException();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}