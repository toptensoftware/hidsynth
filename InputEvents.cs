using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hidsynth
{
    internal class InputEvents : List<InputEvent>
    {
        public void Save(string filename)
        {
            System.IO.File.WriteAllText(filename, string.Join("\n",
                this.Select(x => $"{x.timeStamp},{x.eventId},{x.press}")
            ));
        }

        public void Load(string filename, long offset = 0)
        {
            Clear();
            foreach (var l in System.IO.File.ReadAllLines(filename))
            {
                var parts = l.Split(',');
                if (parts.Length == 3)
                {
                    var e = new InputEvent()
                    {
                        timeStamp = long.Parse(parts[0]) + offset,
                        eventId = int.Parse(parts[1]),
                        press = parts[2] == "True",
                    };
                    this.Add(e);
                }
            }
        }
    }
}
