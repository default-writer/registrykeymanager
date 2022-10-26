///////////////////////////////////////////////////////////////////////////////
// CLASSES
///////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.Versioning;
using System.Collections.Generic;
using Microsoft.Win32;

namespace Registry;

[SupportedOSPlatform("windows")]
public class RegistryKeyManager : IRegistryKeyManager
{
   private Stack<RegistryKey> keys = new Stack<RegistryKey>();

   public IRegistryKey Open(RegistryKey key)
   {
      var wrapper = new RegistryKeyWrapper(this, null, key);
      keys.Push(key);
      return wrapper;
   }

   private IRegistryKeyInternal Open(IRegistryKeyInternal parent, RegistryKey key)
   {
      var wrapper = new RegistryKeyWrapper(this, parent, key);
      keys.Push(key);
      return wrapper;
   }

   public void Dispose()
   {
      while (keys.Count > 0)
      {
         var key = keys.Pop();
         key.Close();
      }
   }

   private interface IRegistryKeyInternal : IRegistryKey
   {
      IRegistryKeyInternal GetParent();
      IRegistryKeyInternal OpenSubKey(IRegistryKeyInternal parent, string key);
      bool HasSubKeys { get; }
      IEnumerable<string> GetSubKeyNames();
      IRegistryKeyInternal GetSubkeyRoot(IRegistryKeyInternal key);
      T TryGetValue<T>(string name = "", T defaultValue = default(T));
      string GetStringValue(string name = "");
      byte[] GetByteArrayValue(string name = "");
   }

   private class RegistryKeyWrapper : IRegistryKeyInternal
   {
      private readonly RegistryKey _key;
      private readonly IRegistryKeyInternal _parent;
      private readonly RegistryKeyManager _manager;

      public RegistryKeyWrapper(RegistryKeyManager manager, IRegistryKeyInternal parent, RegistryKey key)
      {
         _manager = manager;
         _parent = parent;
         _key = key;
      }

      public string Name => _key.Name;

      public string NodeName => _key.Name.Substring(_parent.Name.Length + 1);

      public IEnumerable<string> GetSubKeyNames() => _key.SubKeyCount > 0 ? _key.GetSubKeyNames() : Array.Empty<string>();

      public bool HasSubKeys => _key.SubKeyCount > 0;

      public IRegistryKey OpenSubKey(string subkey) => _manager.Open(this, _key.OpenSubKey(subkey));

      public IRegistryKeyInternal OpenSubKey(IRegistryKeyInternal key, string subkey) => _manager.Open(key, _key.OpenSubKey(subkey));

      public IRegistryKeyInternal GetSubkeyRoot(IRegistryKeyInternal key)
      {
         IRegistryKeyInternal root = _parent;
         if (root != null && root != key)
         {
            do
            {
               root = root.GetParent();
            }
            while (root != null && root != key);
         }
         return root;
      }

      public IRegistryKey Parent { get { return _parent; } }

      public IRegistryKeyInternal GetParent() => _parent;

      public override string ToString()
      {
         return _key.Name;
      }

      private static IEnumerable<IRegistryKeyInternal> GetSubKeys(IRegistryKeyInternal parent, IRegistryKeyInternal key, bool recurse = true)
      {
         foreach (var subkeyName in key.GetSubKeyNames())
         {
            yield return key.OpenSubKey(parent, subkeyName);
            if (recurse)
            {
               foreach (var subkey in GetSubKeys(parent, key.OpenSubKey(parent, subkeyName), recurse))
               {
                  yield return subkey;
               }
            }
         }
      }

      public IEnumerable<IRegistryKey> Children { get => GetSubKeys(this, this, false); }

      public IEnumerable<IRegistryKey> SubKeys { get => GetSubKeys(this, this, true); }

      public IEnumerable<IRegistryKey> Leafs => GetSubKeys(this, this, true).Where(p => !p.HasSubKeys);

      public IEnumerable<IRegistryKey> Branches => GetSubKeys(this, this, true).Where(p => p.HasSubKeys);

      public string StringValue => GetStringValue();

      public byte[] ByteArrayValue => GetByteArrayValue();

      public T TryGetValue<T>(string name = "", T defaultValue = default(T))
      {
         try
         {
            return (T)_key.GetValue(name);
         }
         catch
         {
            return defaultValue;
         }
      }

      public string GetStringValue(string name = "") => TryGetValue(name, "");

      public byte[] GetByteArrayValue(string name = "") => TryGetValue(name, Array.Empty<byte>());
   }
}