class Program{
    // return type is int as you are returning primitive int `0`.
    public int init(){
        //...
        try{
        this.zx();
    }catch(MalformedURLException me){
        // log the Exception here
        // me.printStackTrace();
        // logger.error(... exception message ....);
        throw me;
        // in case you return 0 , in spite of the Exception 
        // you will never know about the exceptional situation
    }
    int retValue = 0;
    return retValue;
    }
}