# Common Marketing Automation Use Cases

Use this document as inspiration when exploring automation opportunities with users. Each use case maps naturally to Xperience's built-in triggers, steps, and actions.

## Lead Nurturing

### Welcome Series
- **Trigger**: Contact submits a registration or sign-up form
- **Value**: Onboards new contacts with a sequence of educational emails
- **Steps**: Send welcome email → Wait 3 days → Send feature highlight → Wait 5 days → Send case study or testimonial
- **Complexity**: Simple

### Re-engagement Campaign
- **Trigger**: Contact has not opened any email in 60+ days (evaluated on a schedule)
- **Value**: Recovers dormant contacts before removing them from active lists
- **Steps**: Send re-engagement email → Wait 7 days → Condition: opened? → True: update engagement attribute → False: send final offer → Update contact as inactive
- **Complexity**: Moderate

## Event Management

### Event Registration Follow-Up
- **Trigger**: Contact submits an event registration form
- **Value**: Confirms attendance and delivers pre-event materials automatically
- **Steps**: Send confirmation email → Wait until 2 days before event → Send logistics reminder → Wait until day after event → Send post-event resources
- **Complexity**: Simple

### Webinar Attendance Nurturing
- **Trigger**: Contact registers for a webinar
- **Value**: Converts webinar interest into sales conversations, differentiating attendees from no-shows
- **Steps**: Send registration confirmation → Send reminder day before → Post-webinar: branch on attended/no-show attribute → Different follow-up track per group
- **Complexity**: Moderate

## Conversions and E-commerce

### Gated Content Download Follow-Up
- **Trigger**: Contact submits a form to download a whitepaper, guide, or template
- **Value**: Moves interested contacts further down the funnel with targeted follow-up
- **Steps**: Send download link email → Wait 3 days → Send related content → Wait 5 days → Send aligned case study → If contact visits pricing page: notify sales rep
- **Complexity**: Moderate

### Post-Purchase Onboarding
- **Trigger**: Contact attribute updated to "Customer" or a purchase event fires
- **Value**: Drives product adoption and reduces early churn
- **Steps**: Send welcome and getting-started guide → Wait 7 days → Send tips email → Wait 14 days → Send check-in survey
- **Complexity**: Simple

### Trial Conversion
- **Trigger**: Contact attribute set to "Trial" (e.g., after signing up for a free trial)
- **Value**: Guides trial users to activation milestones and conversion
- **Steps**: Send trial activation email → Wait 3 days → Condition: activated? → True: send advanced tips → False: send help offer → Wait until trial expiry minus 3 days → Send upgrade prompt
- **Complexity**: Moderate

## Content and Subscription

### Newsletter Subscriber Onboarding
- **Trigger**: Contact subscribes to a blog or newsletter
- **Value**: Establishes brand familiarity and trust before promotional emails
- **Steps**: Send welcome with best content roundup → Wait 1 week → Send most popular articles → Wait 2 weeks → Invite to webinar or event
- **Complexity**: Simple

### Content Stage Progression
- **Trigger**: Contact downloads an awareness-stage asset
- **Value**: Progressively moves contacts from awareness to consideration content
- **Steps**: Update contact attribute (stage = Awareness) → Wait 5 days → Send consideration-stage resource → Wait 5 days → Send decision-stage case study → Notify sales rep if contact is in target segment
- **Complexity**: Moderate

## Internal Operations

### High-Intent Sales Notification
- **Trigger**: Contact visits a pricing page or downloads a sales-relevant asset (contact attribute change or scheduled evaluation)
- **Value**: Ensures sales team follows up on warm leads quickly
- **Steps**: Evaluate contact score or attributes → Condition: above threshold? → True: notify sales rep and update contact stage → False: continue standard nurture
- **Complexity**: Moderate

### Contact Data Quality
- **Trigger**: Scheduled (weekly or monthly)
- **Value**: Keeps the contact database clean and segmentation accurate
- **Steps**: Identify contacts with missing key attributes → Send data-update request email → Wait 7 days → Condition: updated? → True: update status → False: flag for manual review
- **Complexity**: Simple

## Common Patterns

Across all use cases, the most reused elements are:

- **Triggers**: Form submission, contact attribute change, scheduled evaluation
- **Wait steps**: Fixed duration (days/weeks) or condition-based (wait until attribute changes)
- **Condition branching**: Email open/click, attribute value check, scoring threshold
- **Actions**: Send email, update contact attribute, notify a team member
- **Outcomes**: Higher conversion rates, faster follow-up, reduced manual effort, cleaner contact data
