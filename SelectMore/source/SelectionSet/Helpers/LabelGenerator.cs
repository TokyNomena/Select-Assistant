namespace SelectionSet.Helpers
{

    public static class LabelGenerator
    {
        public static string GetLabel(Document document, BuiltInCategory builtInCategory)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            // Get the category from the document
            Category category = Category.GetCategory(document, builtInCategory);

            if (category != null)
            {
                return category.Name;
            }

            // Fallback: Try to get a readable name from the BuiltInCategory enum
            return GetBuiltInCategoryDisplayName(builtInCategory);
        }

        private static string GetBuiltInCategoryDisplayName(BuiltInCategory builtInCategory)
        {
            string enumName = builtInCategory.ToString();

            // Remove "OST_" prefix if present
            if (enumName.StartsWith("OST_"))
            {
                enumName = enumName.Substring(4);
            }

            // Add spaces between words (convert PascalCase to readable text)
            return AddSpacesToSentence(enumName);
        }

        private static string AddSpacesToSentence(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            System.Text.StringBuilder newText = new System.Text.StringBuilder(text.Length * 2);
            newText.Append(text[0]);

            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]) && text[i - 1] != ' ')
                    newText.Append(' ');
                newText.Append(text[i]);
            }

            return newText.ToString();
        }
    }
}
