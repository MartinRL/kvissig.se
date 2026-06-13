---
title: "Choreography or Orchestration? The TODO List Pattern Alternative"
source: "https://www.eventmodelers.ai/docs/blog/choreography-orchestration-newsletter/"
author:
  - "[[Martin Dilger]]"
published: 2025-11-30
created: 2026-06-13
description: "A simpler third option for coordinating distributed processes that's easier to debug and maintain. Learn the TODO List Pattern for process coordination."
tags:
  - "clippings"
---
A simpler third option for coordinating distributed processes that's easier to debug and maintain

![TODO List Pattern](https://www.eventmodelers.ai/assets/images/blog/choreography-orchestration-newsletter.png)

(This was Issue #9 of my weekly Eventmodeling / Evensourcing Newsletter)

Before you dive into this topic - there is a new Online Course “Implementing Eventsourcing”, that will guide you step by step through this process. Find more details at the end of this article.

We all know TODO Lists all too well. And most of use use them in one way or the other. One repeating TODO for me every saturday and sunday - get on a 10 km run (the photo shows the last one)

Speaking of TODO lists, I recently came across a LinkedIn post by Yves Goeleven discussing the age-old debate between Choreography and Orchestration in distributed systems. It sparked the usual discussions about which approach is better. My short answer? How about neither? (more on that later..)

This post reminded me that many people aren’t familiar with a third, simpler alternative: the TODO List Pattern (and yes, you can argue that TODO Lists are a kind of choreography.. and they are a kind of orchestration.. but I don’t want to go into that discussion yet again.. it leads nowhere in my opinion).

Watch a quick video to that shows how it works in just a few minutes.

In any distributed system—whether it’s built with Microservices, Modules, a well-structured Monolith, or even a distributed Monolith—there’s always a need for process coordination.

Take a simple example: your system submits orders, and those orders need to be paid.

![Order and Payment Process](https://www.eventmodelers.ai/assets/images/blog/choreography-orchestration-newsletter-1.png)

In this scenario, two distinct contexts are involved in a single process that needs to be synchronized.

For example, if an order is placed in the Order Service but the payment fails, we cannot confirm the order.

This highlights a common challenge with such processes: who owns the responsibility for coordinating them?

## Orchestration

Using Orchestration to coordinate this process centralizes the ownership of the process. For instance, the “Order Service” itself can manage the logic to update the order status based on payment notifications from the Payment Service.

Alternatively, you could use a dedicated service or deployable, such as an “Order Process Manager.” This component handles all dependencies, including Inventory, Fulfillment, and Tracking.

The “Order Process Manager” is responsible for issuing commands like “Issue Payment” when an order is placed or “Cancel Order” if the payment fails.

**The benefit:** In the beginning, this approach is easier to understand and makes debugging simpler when things go wrong.

**The downside:** Over time, it can become increasingly complex. As more business logic is introduced, different domains (e.g., Payment, Shipping, Fulfillment) can start leaking into the process, complicating its management.

## Choreography

Choreography attempts to eliminate coupling by keeping processes isolated.

In this approach, the “Order Service” submits an order without needing to know who will handle it next. The only thing the Order Service is aware of is this:

- If a “Payment Issued” event is received for a submitted order, the order can be confirmed.
- If a “Payment Failed” event is received for the same order, it needs to be canceled.

The Order Service doesn’t concern itself with the source of these events.

**The benefit:** This approach is more decoupled, making it easier to extend or modify individual services.

**The downside:** It’s harder to debug when things go wrong. For example, if an order remains stuck in the “submitted” state, you’ll need robust monitoring to pinpoint exactly why it’s stuck.

In many cases, the implementation involves some form of a Saga to track the process. However, Sagas come with their own challenges. They can grow increasingly complex and introduce parallel timelines, making it harder to reason about the system. Humans, after all, aren’t great at thinking in parallel timelines.

Which one is best? You might enjoy diving into lengthy discussions about this topic (I know, I sometimes do!), but I’ve realized these debates often miss the point. It’s not about finding the “perfect” approach. Just pick one, start with it, and adapt as needed when it stops working.

But wait… there’s a third option.

## TODO Lists

The TODO List Pattern is simple and often my first choice.

When an order is placed, we use this information to build a TODO list of tasks to complete next. Instead of reacting immediately to an event like “Order Submitted,” we construct a “Payments to Issue” TODO list. This list can be persisted in a database or simply exist in memory—the implementation doesn’t matter.

What does matter is that the TODO list serves as a clearly defined projection of state. The state of the system determines what adds to or removes items from the TODO list.

With this approach, we introduce a single automation process that has one responsibility: periodically checking if there are new TODO items in the list (e.g., payments to be issued). If new items are found, the automation processes them—for example, by calling Stripe to issue a payment.

As with any TODO list, we need a way to check off completed items. This is where the connection between the “Payment Issued” event and the “Payments to Issue” TODO list comes into play. Whenever a “Payment Issued” event is recorded, the corresponding TODO item is marked as resolved or removed from the list.

The automation only activates when new TODO items appear, ensuring efficiency and simplicity.

![TODO List Pattern Flow](https://www.eventmodelers.ai/assets/images/blog/choreography-orchestration-newsletter-2.png)

The biggest advantage of TODO lists is their simplicity and ease of debugging. You can easily visualize them on a debug page and check which TODO lists currently have open items.

Can you see how straightforward it is to model all steps using TODO lists? Take order confirmation as an example: When an order is placed, the TODO list creates an open item for the order that requires payment confirmation. Once the payment is confirmed, the corresponding TODO item is closed.

It’s a simple, readable, and highly effective way to manage processes, isn’t it?

![Complete TODO List Example](https://www.eventmodelers.ai/assets/images/blog/choreography-orchestration-newsletter-3.png)

If done right, the TODO List takes care of most logic for this small part of the system, leaving the automation (the gear symbol) as a simple Task Executor checking off TODOs.

By the way, there is a complete chapter about this Pattern in my Book “Understanding Eventsourcing”.

## Want to learn it faster in a practical way?

The Online Course “Implementing Eventsourcing” gives you exactly that. It’s a perfect companion to the Book “Understanding Eventsourcing”!

Everything I know about Evenet Modeling & Event Sourcing in one course.

**Martin Dilger**

### Learn Event Modeling from the experts

Join the **Event Modeling Hands-On Workshop** on March 16/17 — learn how to design systems that are honest from the start.

**[Register now →](https://nebulit.de/en/eventmodeling-workshop)**