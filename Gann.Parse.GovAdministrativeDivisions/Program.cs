using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;
using FreeSql;
using DataType = FreeSql.DataType;

// See https://www.mca.gov.cn/n156/n186/index.html

Console.WriteLine("Hello, World!");

var areaCode = new Regex(""">(\d+)<\/td>$""", RegexOptions.Multiline);
var areaName = new Regex(""">(.+)<\/td>$""");

Area 省 = null;
Area 市 = null;
var areas = new List<Area>(5000);
var incrId = 1;
{
    var readLines = File.ReadLines("xxx");

    var enumerable = readLines.ToList();
    for (var index = 0; index < enumerable.Count; index++)
    {
        var line = enumerable[index];
        if (!line.EndsWith("></td>") &&
            (line.Contains("class=\"xl7032365\"")
             || line.Contains("class=\"xl7132365\"")))
        {
            var group = areaCode.Match(line).Groups[1];
            if (group.Success)
            {
                var nameNode = enumerable[++index];
                var areaNameEx = areaName.Match(nameNode).Groups[1];
                if (areaNameEx.Success)
                {
                    var area = new Area
                    {
                        Id = incrId++,
                        AreaCode = int.Parse(group.Value),
                        AreaName = areaNameEx.Value[
                                Math.Max(areaNameEx.Value.IndexOf("span>", StringComparison.Ordinal), 0)..]
                            .Replace("span>", "")
                            .Replace("<span style=\"mso-spacerun:yes\">&nbsp;</span>","")
                    };

                    if (!nameNode.Contains("&nbsp;"))
                    {
                        省 = area;
                        市 = area;
                    }
                    else if (nameNode.Contains("&nbsp;") && !nameNode.Contains(">&nbsp;&nbsp;"))
                    {
                        市 = area;
                        area.SuperId = 省.Id;
                        area.ParentId = 省.Id;
                    }
                    else if (市 != null && 省 != null && nameNode.Contains("&nbsp;&nbsp; </span>"))
                    {
                        area.SuperId = 省.Id;
                        area.ParentId = 市.Id;
                    }
                    else if (省 != null && nameNode.Contains(">&nbsp;</span>"))
                    {
                        area.SuperId = 省.Id;
                        area.ParentId = 省.Id;
                    }

                    areas.Add(area);
                }
            }
        }
    }
}
var fsql = new FreeSqlBuilder()
    .UseConnectionString(DataType.MySql,
        @"Data Source=xxx;Port=13306;User ID=xxx;Password=xxx; Initial Catalog=database;Charset=utf8mb4; SslMode=none;Min pool size=1")
    .UseMonitorCommand(cmd => Console.WriteLine($"Sql：{cmd.CommandText}"))
    .UseAutoSyncStructure(true) //自动同步实体结构到数据库，只有CRUD时才会生成表
    .Build();

fsql.Insert<Area>().AsTable(old => "t_gov_administrative_divisions")
    .AppendData(areas)
    .ExecuteAffrows();

public class Area
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    [Column("id")]
    public int Id { get; set; }

    [Column("super_id")] public int? SuperId { get; set; } // 顶层节点，允许为空

    [Column("parent_id")] public int ParentId { get; set; } // 父节点

    [Column("area_code")] public int AreaCode { get; set; } // 行政代码

    [Column("area_name")] public string AreaName { get; set; } // 行政名称
}