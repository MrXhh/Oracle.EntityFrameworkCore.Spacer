using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Oracle.EntityFrameworkCore.Storage.Internal;

namespace Oracle.EntityFrameworkCore
{
    public static class OracleDataBaseExtensions
    {
        /// <summary> 是否忽略大小写
        /// </summary>
        public static bool DataBaseIsIgnoreCase { get; set; } = false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="dataBaseIsIgnoreCase">是否忽略大小写</param>
        /// <returns></returns>
        public static DbContextOptionsBuilder UseOracleDataBaseExtensions(this DbContextOptionsBuilder options, bool dataBaseIsIgnoreCase = false)
        {
            DataBaseIsIgnoreCase = dataBaseIsIgnoreCase;

            options.UseOracleEFCoreSpacer();
            options.ReplaceService<ISqlGenerationHelper, OracleSqlGenerationHelperExtensions>();

            return options;
        }

    }
}
