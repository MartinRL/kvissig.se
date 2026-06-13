---
title: "(20) Learning Event Modeling: Processor-TODO-List pattern"
source: "https://www.linkedin.com/pulse/learning-event-modeling-processor-todo-list-pattern-martin-major-8b26c/"
author:
published: 2001-02-27
created: 2026-06-13
description:
tags:
  - "clippings"
---
#learninginpublic #eventmodeling #eventsourcing

I've recently read "Understanding Eventsourcing" Book by [Martin Dilger](https://www.linkedin.com/in/martindilger/). While the book is an amazing read and helped me to understand Event Modeling and Event Sourcing better, I still couldn't fully grasp the Processor-TODO-List pattern. So I thought the best way to progress is by simply trying to impelment it.

The idea is pretty straight forward: we need to automate a process. Based on some conditions there are certain actions that need to be done. To solve this we create a read model which acts as a todo list. A processor will look at this todo list periodically and based on the state will trigger some actions. The result of these actions will have impact on the todo list, like checking off a task.

Let's just look at an example and go through it step by step. We have a Loan Account that lets users withdraw money until the accounts limit is reached. Users can of course also deposit money to the account. Users might want to request a limit increase. Processing the limit increase request is the problem we want to automate.

So far the events for the LoanAccount event stream looks like this:

![Article content](https://media.licdn.com/dms/image/v2/D5612AQGwM9CQBNtWIQ/article-inline_image-shrink_1000_1488/B56ZVF.xpHGQAU-/0/1740635839562?e=1782950400&v=beta&t=qbUR8fhxIZfyAJ0Ou8_sgrcdUeGmAUS8Jyd_P4sEAwM)

LoanAccount stream

We have 1 green box: LoanAccount which is our decision model. It is used to make decisions, for example checking if the user can still withdraw money without exceeding the limit. We should resist trying to reuse this read model for the limit increase request process! In Event Modeling it is suggested to use seperate green boxes. An event can feed any number of green boxes! Let's update:

![Article content](https://media.licdn.com/dms/image/v2/D5612AQHDiSGBEU4Xng/article-inline_image-shrink_1500_2232/B56ZVF_5sXGQAU-/0/1740636134632?e=1782950400&v=beta&t=H16W07llHiIlXOxZyuGyFrzWlsYHp2Wi5UtzBCCM8NA)

Introducing new read model

The Limit Increase Request event will open the PendingLimitIncreaseRequests TODO List. The processor we will be building will work on this read model. Seeing a Pending Limit Increase Request on the List it can then gather any further required data, translate the data into a AuditLimitIncreaseRequest command and send it to the appropriate handler. Handling the command will result in either a LimitIncreaseGranted or LimitIncreaseRejected event being emitted. These events will than update the TODO List.

Let's look at the implementation:

For this implementation I have chosen Quartz to create a background job running every x seconds. I am using #critterstack aka Wolverine + Marten for event sourcing and messaging in.NET. The Processor is configured to run every x seconds, will fetch any Pending LimitIncreaseRequests. Disallowing concurrent executions prevents us from processing the same request twice. I've come up with some fake busienss logic that requires the total amount deposited in an accounts lifetime so I fetch that data. I translate the data into a command and send it. InvokeAsync means that we wait for the execution of the command. This is important and you'll see why in a minute.

![Article content](https://media.licdn.com/dms/image/v2/D5612AQGIt5ZLr5evwQ/article-inline_image-shrink_1500_2232/B56ZVGJq07GUAc-/0/1740638695500?e=1782950400&v=beta&t=yiNLPHerb0P9Flphdmd6Qu4L3IHzzqqTP0PRUWlaS6s)

The Processor

Next comes the Handler. Wolverine will know from the requests LoanAccountId which LoanAccount (our decision model) should be loaded and pushed into the Handler together with the command. We apply our invariants and then return the events to be emitted to Wolverine to handle I/O concerns for us. This is an example of both the A-Frame architecture as well as the Decider pattern, but I'll leave explanation of that to another article.

![Article content](https://media.licdn.com/dms/image/v2/D5612AQHfOkEf_yV6NQ/article-inline_image-shrink_1500_2232/B56ZVGLDarGoAU-/0/1740639058335?e=1782950400&v=beta&t=gtJh8MyE15dbCiTkPmm8bMuHK3-e4x9OhPU3K_hfJPI)

Wolverines AggregateHandler Workflow

Depending on our business rules either a LimitIncreaseRejected or LimitIncreaseGranted event has been appended to the LoanAccount stream. Let's take a look at our PendingLimitIncreaseRequest projection (our TODO List green box) now:

![Article content](https://media.licdn.com/dms/image/v2/D5612AQGshzae--hOGA/article-inline_image-shrink_1000_1488/B56ZVGMhcdHEAQ-/0/1740639443364?e=1782950400&v=beta&t=pDdNekCck-Zk2RXARJZrQtTN2Al_a-Pp2sTxRomkny4)

Our projection rules

It is pretty straight forward: both events just check it off the list. Remember that these events can also feed any number of other green boxes where different rules apply.

It is important to note, that the Projection is registered as an Inline Projection:

![Article content](https://media.licdn.com/dms/image/v2/D5612AQEjVDwWEmuJGw/article-inline_image-shrink_1500_2232/B56ZVGNIkCHoAg-/0/1740639603382?e=1782950400&v=beta&t=YHIQihZWA5ku2n6xcbQYAd9PTP6mGHtKn24nJqyOlIU)

Article content

Don't worry too much if you are not familiar with Marten, I will cover that in another article. For now, the important thing to note is that Inline Projections are updated in the same transaction as the event is captured and persisted. This ensures Immediate Consistency, which we need in this case. Remember we used InvokeAsync in the Processor to wait for the execution. Disallowing concurrent executions of the Processor + Invoke to wait for the completion of the handler execution + Inline Projection ensures that the next time the Processor runs the system is in a consitent state and the Processor won't work on stale data.

The beauty of the Processor-TODO-List pattern is that it is actually very easy to reason about it. Since you are working based on a state you can reason about what is happening in your system and you can even connect something like an admin dashboard to the Todo List. People more experienced than me claim, that ever since they use this pattern they don't have to bother with Sagas anymore.

I hope you got some value out of this article. I do not claim to be an expert, I am just sharing my learning experience and I hope I learned something modeling and implementing this example.