using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace SocialAI
{
    [TestClass]
    public class Tests
    {

        [TestMethod]
        public void CompletePrompting()
        {
            var content = "** girl flips her  hair --v 4 --ar 2:3 --c 33 --s 123 --seed 33333 ** - <@331647167112413184> (metered, fast)";
            var cleanContent = "girl flips  her hair --v 44 - <Username#1234> (metered, fast)";
            var p = new SocialAi.Prompt(content, cleanContent);
            

            Assert.IsNotNull(p);
            var ann = p.GetAnnotation();
            Assert.AreEqual("girl flips her hair", ann);
            Assert.AreEqual(2, p.AR.Width);
            Assert.AreEqual(3, p.AR.Height);
            Assert.AreEqual(33, p.Chaos);
            Assert.AreEqual(33333, p.Seed);
            Assert.AreEqual(123, p.Stylize);
            Assert.AreEqual(4, p.Version);
            Assert.AreEqual("Username", p.DiscordUser.DiscordUsername);
        }

        [TestMethod]
        public void DefalutPrompting()
        {
            var content = "** girl flips her  hair ** - <@331647167112413184> (metered, fast)";
            var cleanContent = "girl flips  her hair  - <Username#1234> (metered, fast)";
            var p = new SocialAi.Prompt(content, cleanContent);

            Assert.IsNotNull(p);
            var ann = p.GetAnnotation();
            Assert.AreEqual("girl flips her hair", ann);
            Assert.IsNull(p.AR);
            Assert.AreEqual(null, p.Chaos);
            Assert.AreEqual(null, p.Seed);
            Assert.AreEqual(null, p.Stylize);
            Assert.AreEqual(null, p.Version);
            Assert.AreEqual("Username", p.DiscordUser.DiscordUsername);
        }
    }
}
