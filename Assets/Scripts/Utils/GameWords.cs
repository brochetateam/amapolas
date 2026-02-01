using UnityEngine;

namespace Amapolas.Utils
{
    public static class GameWords
    {
        public static readonly string[] HurtfulWords = new string[]
        {
            "NO PUEDES",
            "NO ERES SUFICIENTE",
            "ESTÁS SOLO",
            "ES TU CULPA",
            "NADA IMPORTA",
            "RÍNDETE",
            "NUNCA CAMBIARÁS",
            "TE JUZGAN",
            "TE MIRAN",
            "FALLARÁS",
            "NADIE TE ENTIENDE"
        };

        public static readonly string[] KindWords = new string[]
        {
            "TÚ PUEDES",
            "ERES VALIENTE",
            "SIGUE ADELANTE",
            "NO ESTÁS SOLO",
            "TIENES VALOR",
            "ERES ÚNICO",
            "CONFÍA EN TI",
            "RESPIRA",
            "TODO PASARÁ",
            "ERES FUERTE",
            "ESTAMOS AQUÍ"
        };

        public static string GetRandomHurtful() => HurtfulWords[Random.Range(0, HurtfulWords.Length)];
        public static string GetRandomKind() => KindWords[Random.Range(0, KindWords.Length)];
    }
}
