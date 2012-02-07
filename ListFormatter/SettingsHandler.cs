using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.IO;
namespace WindowsFormsApplication1 {
    class SettingsHandler: ConfigFileHandler {
        public SettingsHandler(): base(Directory.GetCurrentDirectory(),"config.xml") {
            loadSettings();
        }

        public bool deleteCustomFormat(string name) {
            return this.clearSpecificNode("format", "name", name);
        }

        public List<string> getCustomFormatNames() {
            List<string> names = new List<string>();
            foreach(XmlElement node in this.getNodeGroup("format")) {
                if (node.HasAttribute("name")) {
                    names.Add(node.GetAttribute("name"));
                }
            }
            return names;
        }

        public string getCustomFormat(String name) {
            return this.getSpecificNodeValue("format", "name", name);
        }

        public bool saveCustomFormat(string name, string format) {
            this.setSpecificNodeValue("format", format, "name", name);
            this.loadSettings();
            return true;
        }
    }
}
