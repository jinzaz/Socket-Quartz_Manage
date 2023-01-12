using System.Text;

namespace Socket_Quartz_Manage.Extensions
{
    public static class ByteExtensions
    {
        /// <summary>
        /// 字节数组转16进制字符串：空格分隔
        /// </summary>
        /// <param name="byteDatas"></param>
        /// <returns></returns>
        public static string ToHexStrFromByte(this byte[] byteDatas)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < byteDatas.Length; i++)
            {
                builder.Append(string.Format("{0:X2} ", byteDatas[i]));
            }
            return builder.ToString().Trim();
        }

        /// <summary>
        /// 8进制转16
        /// </summary>
        /// <returns></returns>
        public static string To16(int num)
        {
            var str = Convert.ToString(num, 16);

            return str.Length == 1 ? "0" + str : str;
        }
    }
}
