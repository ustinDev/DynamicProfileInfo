using System;

namespace ProfileInfo.Services.Interfaces
{
    public interface ICatFactService
    {
        Task<string> GetRandomCatFactAsync();
    }
}
