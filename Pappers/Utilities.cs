using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Papers {
    static class AutoCompleteHelper {

        static internal Dictionary<string, object> GetItem(string label, object value) {

            return new Dictionary<string, object>() { { "label", label }, { "value", value } };
        }

        static internal Dictionary<string, object> GetItem(string label, object value, string type) {

            return new Dictionary<string, object>() { { "label", label }, { "value", value }, { "type", type } };
        }
    }
}
