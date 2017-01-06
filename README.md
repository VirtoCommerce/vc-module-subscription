# VirtoCommerce.Subscription
VirtoCommerce.Subscription module represents subscriptions and recurring orders management system. It enables retailers to sell subscription-based offerings and shoppers to place recurring orders online.

Key features:
* trial periods
* grace periods
* free subscriptions

# Recurring order scenario
1. Customer adds products to cart as usual and proceeds to checkout
2. Customer selects "I want this to be a recurring order" option
3. Customer sets the recurrence parameters
4. Customer proceeds with the checkout and submits the order
5. System creates both the order and a subscription based on that order
6. System background job periodically checks existing subscriptions and generates new orders when needed
7. System sends email notification to the customer about the new order
8. Customer receives the email and clicks the link to open the order in storefront.
9. a) Customer reviews the order and confirms payment
9. b) Alternatively, customer opens the associated subscription and cancels it. No more new orders would be generated for this subscription ever again.

![Recurring order workflow](https://cloud.githubusercontent.com/assets/5801549/21717221/4dace7d0-d418-11e6-8688-56866b71be27.png)

# Documentation

# Installation
Installing the module:
* Automatically: in VC Manager go to Configuration -> Modules -> Subscription module -> Install
* Manually: download module zip package from https://github.com/VirtoCommerce/vc-module-subscription/releases. In VC Manager go to Configuration -> Modules -> Advanced -> upload module package -> Install.

# Settings
* **Subscription.EnableSubscriptions** - Flag for activating subscriptions in store;
* **Subscription.Status** - Subscription statuses (Trialling, Active, Cancelled, Expired, etc.);
* **Subscription.SubscriptionNewNumberTemplate** - The template (pattern) that will be used to generate the number for new Subscription. Parameters: 0 - date (the UTC time of number generation); 1 - the sequence number;
* **Subscription.CronExpression** - cron expression for scheduling subscription processing job execution.

# Available resources
* API client documentation http://demo.virtocommerce.com/admin/docs/ui/index#!/Subscription_module

# License
Copyright (c) Virtosoftware Ltd.  All rights reserved.

Licensed under the Virto Commerce Open Software License (the "License"); you
may not use this file except in compliance with the License. You may
obtain a copy of the License at

http://virtocommerce.com/opensourcelicense

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
implied.
