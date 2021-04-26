using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Microsoft.BotBuilderSamples
{
    public class ShoppingItem
    {
        // Name of the item
        public string name { get; set; }

        // Quantity of the item a user wishes to purchase in the future
        public string quantity { get; set; }

        // Price of the item based on the quantity of the item
        public string price { get; set; }
    }
}
