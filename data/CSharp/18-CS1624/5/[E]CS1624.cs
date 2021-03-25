// CS1624.cs  
using System;  
using System.Collections;  
  
class C  
{  
    void Start () 
    {   
        string dataUrl = "http://www.my-site.com/game/test.php";
        string playName = "Player 1";
        int score = -1;

        // Create a form object for sending high score data to the server
        var form = new WWWForm();
        // Assuming the perl script manages high scores for different games
        form.AddField( "game", "MyGameName" );
        // The name of the player submitting the scores
        form.AddField( "playerName", playName );
        // The score
        form.AddField( "score", score );

        // Create a download object
        WWW downloadW = new WWW( dataUrl, form );

        // Wait until the download is done
        yield return downloadW;


        if(downloadW.error == null) {
            print( "Error downloading: " + downloadW.error );
            return false;
        } else {
            // show the highscores
            Debug.Log(downloadW.text);
        }
    }
} 