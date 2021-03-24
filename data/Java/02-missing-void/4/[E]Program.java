import javax.swing.* ;
import java.awt.event.* ;   

class LanguageExplorerJava extends JFrame implements ActionListener
{
    JPanel Q1 = new JPanel() ;

    JButton Q1A1 = new JButton( "Bonjour" ) ;  
    JButton Q1A2 = new JButton( "Salut" ) ;
    JButton Q1A3 = new JButton( "Piscine" ) ;

    public Q1LanguageExplorerJava()
    {
        super( "Q1 - What is hello in French" );
        setSize( 500,200 );
        setDefaultCloseOperation( EXIT_ON_CLOSE );
        add(Q1);

        Q1.add( Q1A1 ) ;           
        Q1.add( Q1A2 ) ;
        Q1.add( Q1A3 ) ;

        Q1A1.addActionListener(this);      
        Q1A2.addActionListener(this);
        Q1A3.addActionListener(this);

        setVisible( true );
    }

    public void actionPerformed( ActionEvent event )
    {
        if( event.getSource() == Q1A1)
        {
            JOptionPane.showMessageDialog( this,"CORRECT","Message Dialog",JOptionPane.INFORMATION_MESSAGE );
            int score = 0;
            score = score+=1;
            System.out.println(score); 
        }
        if( event.getSource() == Q1A2)
        JOptionPane.showMessageDialog( this,"INCORRECT","Message Dialog",JOptionPane.INFORMATION_MESSAGE); 

        if( event.getSource() == Q1A3)
        JOptionPane.showMessageDialog( this,"INCORRECT","Message Dialog",JOptionPane.INFORMATION_MESSAGE);  
    }
     public static void main( String[] args )
     {
         Q1LanguageExplorerJava gui = new Q1LanguageExplorerJava();
     }
}