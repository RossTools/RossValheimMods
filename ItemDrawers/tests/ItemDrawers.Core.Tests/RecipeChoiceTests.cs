using System.Collections.Generic;
using ItemDrawers.Core;
using Xunit;

namespace ItemDrawers.Core.Tests
{
    public class RecipeChoiceTests
    {
        private static readonly (string, int)[] Default = { ("FineWood", 10) };

        private static bool Vanilla(string item) =>
            new HashSet<string> { "FineWood", "Wood", "Stone" }.Contains(item);

        [Fact]
        public void A_valid_configured_recipe_replaces_the_default()
        {
            var (reqs, problem) = RecipeChoice.Choose("Wood:10", Default, Vanilla);
            Assert.Null(problem);
            Assert.Equal(new[] { ("Wood", 10) }, reqs);
        }

        // Issue #22's server edit, applied as Jotunn delivers it: the entry
        // first holds the player's own text, then the server's. Each answer
        // depends only on the text it is given, so the later one wins.
        [Fact]
        public void Choosing_again_with_the_servers_text_gives_the_servers_recipe()
        {
            var local = RecipeChoice.Choose("FineWood:10", Default, Vanilla);
            var server = RecipeChoice.Choose("Wood:10,Stone:2", Default, Vanilla);

            Assert.Equal(new[] { ("FineWood", 10) }, local.Requirements);
            Assert.Equal(new[] { ("Wood", 10), ("Stone", 2) }, server.Requirements);
        }

        [Fact]
        public void Unparseable_text_falls_back_and_says_why()
        {
            var (reqs, problem) = RecipeChoice.Choose("Wood x10", Default, Vanilla);
            Assert.Equal(Default, reqs);
            Assert.Contains("Item:Count", problem);
        }

        [Fact]
        public void An_unknown_item_falls_back_and_names_it()
        {
            var (reqs, problem) = RecipeChoice.Choose("Fine Wood:10", Default, Vanilla);
            Assert.Equal(Default, reqs);
            Assert.Equal("no item named 'Fine Wood'", problem);
        }

        [Fact]
        public void With_no_way_to_check_items_the_recipe_is_let_through()
        {
            var (reqs, problem) = RecipeChoice.Choose("Anything:3", Default, null);
            Assert.Null(problem);
            Assert.Equal(new[] { ("Anything", 3) }, reqs);
        }
    }
}
