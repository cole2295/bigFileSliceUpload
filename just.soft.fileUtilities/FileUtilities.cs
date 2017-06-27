using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace just.soft.fileUtilities
{
    public class FileUtilities
    {
        private string slicePath = @"E:\uploaded\slices\";
        private string loadedPath = @"E:\uploaded\";
        public async Task<int> UploadFileAsync(HttpContext context)
        {
            int index = -1;
            if (context.Request.HasFormContentType)
            {
                var form = context.Request.Form;//获取请求Form
                var isLast = form["isLast"] + string.Empty;              //前端传输是否为切割文件最后一个小文件
                var count = form["count"] + string.Empty;                //前端传输当前为第几次切割小文件
                var fileName = form["name"] + string.Empty;              //获取前端处理过的传输文件名

                if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(count)) return index;
                int.TryParse(count, out index);

                string fileRawName = fileName.Substring(0, fileName.LastIndexOf('.'));//分片文件原始名
                string fileSliceFullDir = Path.Combine(slicePath, fileRawName);//分片文件目录
                string fileSliceFullName = Path.Combine(fileSliceFullDir, fileName);//分片文件完全限定名
                FileStream fileOut = null;//文件输出流
                string[] allFile = null;
                foreach (var file in form.Files)
                {
                    //保存切割文件到磁盘
                    if (!Directory.Exists(fileSliceFullDir)) Directory.CreateDirectory(fileSliceFullDir);
                    if (File.Exists(fileSliceFullName))
                    {
                        File.Delete(fileSliceFullName);
                    }
                    fileOut = new FileStream(fileSliceFullName, FileMode.CreateNew, FileAccess.ReadWrite);
                    await file.CopyToAsync(fileOut)
                        .ContinueWith(_ => { fileOut.Dispose(); })
                        .ConfigureAwait(false);
                }
                if (bool.TryParse(isLast, out bool isLastB) && isLastB)
                {
                    string fileLoadedFullName = Path.Combine(loadedPath, fileRawName);//完整文件保存路径
                    if (File.Exists(fileLoadedFullName)) File.Delete(fileLoadedFullName);//先删除,否则新文件就不能创建
                    fileOut = new FileStream(fileLoadedFullName, FileMode.CreateNew, FileAccess.ReadWrite);//创建新文件流
                    allFile = Directory.GetFiles(fileSliceFullDir);//获取所有切割文件
                    allFile = allFile.OrderBy(s => int.Parse(Regex.Match(s, @"\d+$").Value)).ToArray();//切割文件顺序排序
                    FileStream fileIn = null;
                    for (int i = 0; i < allFile.Length; i++)//拼接文件
                    {
                        fileIn = new FileStream(allFile[i], FileMode.Open);
                        await fileIn.CopyToAsync(fileOut)
                            .ContinueWith(_ => { fileIn.Dispose(); })
                            .ConfigureAwait(false);
                    }
                    fileOut.Dispose();
                }
            }
            return ++index;
        }
    }
}
