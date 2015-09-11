namespace RxSample
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface INameService
    {
        Task<IEnumerable<string>> GetNames();
    }
}
