class Job {
    public boolean runAJob() {
        boolean jobsFinished = false;
        Job firstJob = getCurrentJob();
        String jobName = firstJob.getName();
        int jobDuration = firstJob.getDuration();
        if (!myJob.isEmpty()&& jobDuration > 0 && jobDuration <= myTotalDuration) { 
            myTotalDuration -= jobDuration;
            myFinishedJobs.add(myJob.get(0));
            myJob.remove(0);
            System.out.println("'"+jobName+"'" +" is completed. "+ myTotalDuration+ " seconds remaining on the clock");
            jobsFinished = true;
        } else {
            System.out.println("JobQueue is empty");
        }
       return jobsFinished;
    } 
}