/*
 * IsExternalInit is not defined in .NET Standard 2.1, so records cannot be used.
 * 
 * We add a dummy class ourself to satisfy compilation, but not in versions of .NET where IsExternalInit *is* defined
 */

#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices;

internal static class IsExternalInit { }
#endif