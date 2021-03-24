class Deck {
    public Deck () {
        deck = new Card();
        int cardcount = 0;
    }
     public void buildDeck () {
        for (Card.Rank rankit: Card.Rank.values()) {
            for (Card.Suit suitit: Card.Suit.values()) {
                this.deck[cardcount] = new Card(suitit,rankit);
                cardcount++;
            };
        };
    };
    Card deck;
}