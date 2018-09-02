using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace client_okta
{
    public interface ITokenService
    {
        Task<string> GetToken();
    }

    public interface IApiService
    {
        Task<IList<string>> GetValues();
    }
}
