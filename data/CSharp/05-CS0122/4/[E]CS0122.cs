using System;

namespace KeyWord
{
    public class KeyWord
    {
        private String[] keywords = { "abstract", "as", "etc." };

    }
}
public class Program
{
    public static void Main()
    {
        // Check whether the token is a keyword. 
        var keyboardCls = new KeyWord.KeyWord();
        String[] keywords = keyboardCls.keywords;

        for (int i = 0; i < keywords.Length; i++)
        {
            /* NOT IMPORTANT
            if (keywords[i] == token)
            {
                // Apply alternative color and font to highlight keyword.        
                rtb.SelectionColor = Color.Blue;
                rtb.SelectionFont = new Font("Courier New", 10, FontStyle.Bold);
                break;
            }
            */
        }
    }
}
