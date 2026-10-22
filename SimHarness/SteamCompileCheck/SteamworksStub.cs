// A stand-in for the parts of Steamworks.NET (https://github.com/rlabrecque/Steamworks.NET, MIT) that the Steam
// module uses, with the same names and signatures as Steamworks.NET 2025.164.1. It exists only so the module can be
// type-checked outside Unity; it is never part of the game and does nothing.
using System;

namespace Steamworks
{
    public enum EResult
    {
        k_EResultOK = 1,
        k_EResultFail = 2,
    }

    public struct AppId_t
    {
        public uint m_AppId;
        public AppId_t(uint value) { m_AppId = value; }
    }

    public struct CSteamID
    {
        public ulong m_SteamID;
    }

    public struct HAuthTicket : IEquatable<HAuthTicket>
    {
        public static readonly HAuthTicket Invalid = new HAuthTicket(0);
        public uint m_HAuthTicket;
        public HAuthTicket(uint value) { m_HAuthTicket = value; }
        public static bool operator ==(HAuthTicket x, HAuthTicket y) => x.m_HAuthTicket == y.m_HAuthTicket;
        public static bool operator !=(HAuthTicket x, HAuthTicket y) => !(x == y);
        public bool Equals(HAuthTicket other) => m_HAuthTicket == other.m_HAuthTicket;
        public override bool Equals(object other) => other is HAuthTicket ticket && Equals(ticket);
        public override int GetHashCode() => m_HAuthTicket.GetHashCode();
    }

    public struct GetTicketForWebApiResponse_t
    {
        public HAuthTicket m_hAuthTicket;
        public EResult m_eResult;
        public int m_cubTicket;
        public byte[] m_rgubTicket;
    }

    public static class Packsize
    {
        public static bool Test() => true;
    }

    public static class SteamAPI
    {
        public static bool Init() => false;
        public static void Shutdown() { }
        public static bool RestartAppIfNecessary(AppId_t unOwnAppID) => false;
        public static void RunCallbacks() { }
    }

    public static class SteamUser
    {
        public static CSteamID GetSteamID() => default;
        public static bool BLoggedOn() => false;
        public static HAuthTicket GetAuthTicketForWebApi(string pchIdentity) => default;
        public static void CancelAuthTicket(HAuthTicket hAuthTicket) { }
    }

    public enum EOverlayToStoreFlag
    {
        k_EOverlayToStoreFlag_None = 0,
        k_EOverlayToStoreFlag_AddToCart = 1,
        k_EOverlayToStoreFlag_AddToCartAndShow = 2,
    }

    public static class SteamFriends
    {
        public static string GetPersonaName() => "";
        public static void ActivateGameOverlayToStore(AppId_t nAppID, EOverlayToStoreFlag eFlag) { }
    }

    public static class SteamUtils
    {
        public static bool IsOverlayEnabled() => false;
    }

    public static class SteamUserStats
    {
        public static bool GetAchievement(string pchName, out bool pbAchieved)
        {
            pbAchieved = false;
            return false;
        }

        public static bool SetAchievement(string pchName) => false;
        public static bool StoreStats() => false;
    }

    public static class SteamRemoteStorage
    {
        public static bool FileWrite(string pchFile, byte[] pvData, int cubData) => false;
        public static int FileRead(string pchFile, byte[] pvData, int cubDataToRead) => 0;
        public static bool FileExists(string pchFile) => false;
        public static int GetFileSize(string pchFile) => 0;
        public static long GetFileTimestamp(string pchFile) => 0;
        public static bool IsCloudEnabledForAccount() => false;
        public static bool IsCloudEnabledForApp() => false;
    }

    public abstract class Callback
    {
    }

    public sealed class Callback<T> : Callback, IDisposable
    {
        public delegate void DispatchDelegate(T param);
        public static Callback<T> Create(DispatchDelegate func) => new Callback<T>();
        public void Dispose() { }
    }
}
