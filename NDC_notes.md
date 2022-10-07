# NDC 2022

## Day 1
### Keynote: Managing the Burnout Burndown
Talk about it is simple to burn out your self as a developer.  

* Learn your body
* What do you need to do to make sure not get low on batteries. Hard to just do as someone else. We are all different and have different surroundings.

* Don´t worry to much about things that is not probably going to happen. Think how likely it is going to happen.
* Get some sleep. Sleep your hours every night.
* Exercise. Running and gym is good as and example.
* Eat well
* Chew properly
* Look around once in a while, otherwise you can miss it! Take in the present.
* Think and analyze
  
  
### Observable Web Applications - Todd Gardner
Hur skall man kunna ha kontroll på hur din webapplikation mår och fungerar.  
  
Mindre webbapplikationer har lättare att ha kontroll än stora webbapplikationer som är omöjligt för en person att ha övergrippande kontroll.  

Man bör ha bättre context information i sinna loggar. Detta för att man skall förstå vilken URL problemet uppstod. Vem är inloggad? etc.      

Performance påverkar page-rank.  
Google 
* First Contentfull Paint (FCP)
* First Input Delay (FID)
* Largest Contentfull Page (LCP)
* Cumulative Layout Shift (CLS)     
https://developers.google.com/speed/docs/insights/v5/about

Tips på hur man kan upptäka avvikelser enklare än :  
* Meridian (för att se när den ändras).
* Percentil 70 och 95
* Current vs previous. Titta och se skillnaden emot tidigare period.

Analysera hur användare upplever hur det fungerar och ha Kpi´er. Ex checkout i en webshop har gått ned från 25% => 18%. Något är fel.  

### Oauth
Nu fattar jag. Har byggt frukostfrallan-cli
  
### Take control of your cloud environment using IaC with Pulumi

IaC with Pulumi. 
Go Docker or web apps in Azure?

Tomas visade lite lätt hur enkelt du kan jobba med Pulumi.

När skall man köra Pulumi. Small projekt, enterprise. Från portal i våran DevOps.

Saknar mer visuell magi. För mycket Cli och för lite resultat.
Typisk dålig dragning....


### Consuming GraphQL using C#
GraphQL  
!paas graphql api. Page in Azure DevOps som installeras som extension.
Banans cake pop

## Day 2

### Refactoring Is Not Just Clickbait
"Refactoring is a disciplined technique for restructuring an existing body of code, altering its internal structure without changing its external behavior."  – Martin Fowler

Managed Technical debt  
* It is ok with technical debt (depending of the situation).
* You should refactor when the technical debt is a problem.
* Refactoring should have a meaning. Performance/security/readable code
* Hard and risky to refactor code that has low test coverage
  
Legacy  
* Do not save functions/code because of it always been there. 
* Make sure that your team understand what the code does.
* Optimize your code
* Make sure that you delete code that is not used.

Mindset
* Include refactoring in your everyday job. Don´t do it as a thing in the end of the month. (It is like not showoring and change cloths for a long time. You will smell.)
* Don´t just look on small functions. They often looks ok. "Put everything in the same room". Make sure that you understand the big picture of the functionality and how things are connected. Then break it down to smaller pices.
* Balance to write short code in the same time make it simple to understand what it does.


### I'm just trying to keep my head above water
Chris klug - active solution  
Talked about his mental health and ADHD "diagnosis".  

* Imposter syndrome
* Help your self with train, sleep, and eat well.
* Accept your problem 

* HyperFocus - forget time and surrounding

* Not a diagnosis. Everybody is more or less on the "Adhd, bipolär, autism" spectrum
* Learn, understand, adapt
* Know your strengths and weaknesses.
* Have short and long term goals when you work with your strengths and weaknesses



### Failure is Always an Option
Talked about the project to put a man on the moon.  

* The number of 
Peopleware 
Userstory: return safe to earth 
Blixten slog ned 
Backup 

Orbiter, 

Vana gör att vi inte ser saker som en risk. Man blir för kaxig.

Svårt att förutse alla undantags grejor som kan hända.

Kaos - 

Mopesa 

Lyssna och titta på hur användarna använder din kod.


### Effective DevOps for Organizations
* It is not wrong on DevOps. It is used incorrectly. Wrong implementation.
* Work with small changes

* I left the talk. Not a good and inspiring talk.



### What's new in C#? Exciting new features in C# 8.0, 9.0 and 10!
* Interface default methods
* Nullable reference types
* Async stream
* Yield???
* Pattern matching. Demo under en fredag. 
* Global using NDC; (namespace). Namespace NDC;

### .NET Rocks Live: Making Open Source Work for Everyone
Recording of Podcast about .NET  
* Passive aggressive pull requests


## Day 3

### Keynote 
Advent of code
Writing instructions 
Fullstack is impossible
Hon pratar om min känsla om utveckling. Det går för fort. Behöver vi allt nytt. Behöver vi verkligen skriva om allt hela tiden för att möte upp till nya framework/ products 
Spendera mindre tid på ny framework. Spendera tid på att lära basic/ kunna återvända kod.

* Spend less time on new frameworks. Spend time on learning the basic and make sure to reuse code.

### Monitoring and alerting like a pro with Azure Monitor/Application Insights
Really good talk about Azure Insights.  

* "Grafana". Service to show graphs. 
* Showed how simple it can be used in applications.
* Showed how we simple can setup availability checks. Send mails etc.
* Maybe we could reach the Insights info from our DXP customers and be more proactive.
* KQL Looked cool but can not remeber how it looks like
* Install Azure app on your phone

### Down the Oregon Trail with Functional C#
Showed and talked about how he created Oregon trail as a console application with functional c#.  
Inspiring!

### How I work with JSON
One hour talk about JSON. Really? .... yes...
* Interface contract
* JSON schema
* Njsonschema


## Other
### Eisenhower's Principle
Eisenhower's Urgent/Important Principle helps you quickly identify the activities that you should focus on, as well as the ones you should ignore.  
  
When you use this tool to prioritize your time, you can deal with truly urgent issues, at the same time as you work towards important, longer-term goals.  
  
To use the tool, list all of your tasks and activities, and put each into one of the following categories:  
* Important and urgent.
* Important but not urgent.
* Not important but urgent.
* Not important and not urgent.
Then schedule tasks and activities based on their importance and urgency.

https://www.mindtools.com/pages/article/newHTE_91.htm#:~:text=Eisenhower%2C%20who%20was%20quoting%20Dr,organized%20his%20workload%20and%20priorities.

-----

  
! Anders Wahlström - reset application/website  
  
Oregon trail  
  
Akvarium - mat, jakt / slagsmål
