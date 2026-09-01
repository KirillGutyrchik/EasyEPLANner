using System.Collections.Generic;

namespace TechObject
{
        /// <summary>
        /// Переносимые ссылки на привязанные агрегаты: base_tech_object
        /// и технологический номер вместо глобальных индексов.
        /// </summary>
    public static class AttachedObjectReferences
    {
        public const string LuaFieldName = "attached_objects_refs";

        public static string FromIndices(ITechObjectManager manager,
            string indicesValue)
        {
            if (string.IsNullOrWhiteSpace(indicesValue))
                return string.Empty;

            var refs = new List<string>();
            foreach (string part in indicesValue.Split(' '))
            {
                if (!int.TryParse(part, out int index))
                    continue;

                TechObject attachedObject = manager.GetTObject(index);
                if (attachedObject?.BaseTechObject == null)
                    continue;

                refs.Add(FormatReference(attachedObject));
            }

            return string.Join(" ", refs);
        }

        public static string FormatReference(TechObject techObject)
        {
            return $"{techObject.BaseTechObject.EplanName}:" +
                $"{techObject.TechNumber}";
        }

        public static List<int> ToIndices(ITechObjectManager manager,
            string refsValue)
        {
            var indices = new List<int>();
            if (string.IsNullOrWhiteSpace(refsValue))
                return indices;

            foreach (string part in refsValue.Split(' '))
            {
                if (string.IsNullOrWhiteSpace(part))
                    continue;

                string[] segments = part.Split(':');
                int techNumber;
                string baseObjectName;

                if (segments.Length == 2)
                {
                    baseObjectName = segments[0];
                    if (!int.TryParse(segments[1], out techNumber))
                        continue;
                }
                else if (segments.Length == 3)
                {
                    // Старый формат base:tech_type:n — tech_type игнорируется.
                    baseObjectName = segments[0];
                    if (!int.TryParse(segments[2], out techNumber))
                        continue;
                }
                else
                {
                    continue;
                }

                int index = manager.GetTechObjectN(baseObjectName, techNumber);
                if (index > 0)
                    indices.Add(index);
            }

            return indices;
        }
    }
}
