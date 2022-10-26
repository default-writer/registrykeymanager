///////////////////////////////////////////////////////////////////////////////
// INTERFACES
///////////////////////////////////////////////////////////////////////////////

using Microsoft.Win32;

namespace Registry;

public interface IRegistryKey
{
   string Name { get; }
   string NodeName { get; }
   IRegistryKey OpenSubKey(string key);
   IRegistryKey Parent { get; }
   IEnumerable<IRegistryKey> Children { get; }
   IEnumerable<IRegistryKey> SubKeys { get; }
   IEnumerable<IRegistryKey> Leafs { get; }
   IEnumerable<IRegistryKey> Branches { get; }
   string StringValue { get; }
   byte[] ByteArrayValue { get; }
}

public interface IRegistryKeyManager : IDisposable
{
   IRegistryKey Open(RegistryKey key);
}
