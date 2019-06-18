using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DaedalustLib.Tests
{
    [TestClass]
    public class ParserTest
    {
        [TestMethod]
        public void Parse()
        {
            const string code = @"func void Ninja_ManaReg_Regeneration() {
    Npc_GetInvItem()
    // Not during loading
    if (!Hlp_IsValidNpc(hero)) { return; };
    // Only in-game
    if (!MEM_Game.timestep) { return; };
    // Only in a certain interval
    var int delayTimer; delayTimer += MEM_Timer.frameTime;
    if (delayTimer < DEFAULT_NINJA_MANAREG_TICKRATE) { return; };
	
    delayTimer -= DEFAULT_NINJA_MANAREG_TICKRATE;
    
    if (hero.attribute[ATR_MANA_MAX] >= Ninja_ManaReg_Mana_Threshold) {
        if (hero.attribute[ATR_MANA] < hero.attribute[ATR_MANA_MAX]) {
            var int menge; menge = (hero.attribute[ATR_MANA_MAX] + (Ninja_ManaReg_Max_Mana_Divisor/2)) / Ninja_ManaReg_Max_Mana_Divisor;
            Npc_ChangeAttribute(hero, ATR_MANA, menge);
        };
    };
};";
            var parserResult = DaedalusLib.Parser.DaedalusParserHelper.Parse(code);
        }
    }
}
