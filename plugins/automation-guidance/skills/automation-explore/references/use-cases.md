# Common Marketing Automation Use Cases

Use this document as inspiration when exploring automation opportunities with users. Each use case maps to one of Xperience's three trigger types.

**Trigger types reference:**
- **Form submission** — a specific form is submitted; marketer-configurable
- **Registration** — a member account becomes active/enabled; marketer-configurable
- **Custom activity** — a developer-registered activity is logged for the contact; requires developer setup, but once created the marketer can use it freely

Use cases marked _(requires developer)_ need a custom activity type created and logged by code before the marketer can build the automation.

---

## Lead Nurturing

### Welcome Series
- **Trigger**: Form submission — contact submits a registration or sign-up form
- **Value**: Onboards new contacts with a sequence of educational emails
- **Steps**: Send welcome email → Wait 3 days → Send feature highlight → Wait 5 days → Send case study or testimonial
- **Complexity**: Simple

### Re-engagement Campaign _(requires developer)_
- **Trigger**: Custom activity — logged by a scheduled job that identifies contacts with no email engagement in 60+ days
- **Developer setup**: Scheduled background job evaluates email engagement data and logs a custom activity (e.g., `contact_gone_dormant`) for qualifying contacts
- **Value**: Recovers dormant contacts before removing them from active lists
- **Steps**: Send re-engagement email → Wait 7 days → Condition: opened? → True: set engagement attribute → False: send final offer → Set contact as inactive
- **Complexity**: Moderate

## Event Management

### Event Registration Follow-Up
- **Trigger**: Form submission — contact submits an event registration form
- **Value**: Confirms attendance and delivers pre-event materials automatically
- **Steps**: Send confirmation email → Wait until 2 days before event → Send logistics reminder → Wait until day after event → Send post-event resources
- **Complexity**: Simple

### Webinar Attendance Nurturing _(requires developer)_
- **Trigger**: Form submission — contact submits a webinar registration form; post-webinar branching requires a custom activity (e.g., `webinar_attended`) logged by the webinar platform or a developer-built integration
- **Developer setup**: Log a custom activity after attendance is confirmed (e.g., via webhook from the webinar platform)
- **Value**: Converts webinar interest into sales conversations, differentiating attendees from no-shows
- **Steps**: Send registration confirmation → Send reminder day before → Wait until day after → Condition: `webinar_attended` activity logged? → Attendee track vs. no-show track → Finish
- **Complexity**: Moderate

## Conversions and E-commerce

### Gated Content Download Follow-Up
- **Trigger**: Form submission — contact submits a gated content download form
- **Value**: Moves interested contacts further down the funnel with targeted follow-up
- **Steps**: Send download link email → Wait 3 days → Send related content → Wait 5 days → Send aligned case study
- **Complexity**: Simple

### Post-Purchase Onboarding _(requires developer)_
- **Trigger**: Custom activity — logged when a purchase is completed (e.g., `purchase_completed`); alternatively, Form submission if a post-purchase confirmation form exists
- **Developer setup**: Log a custom activity from the purchase flow or an external e-commerce system via webhook
- **Value**: Drives product adoption and reduces early churn
- **Steps**: Send welcome and getting-started guide → Wait 7 days → Send tips email → Wait 14 days → Send check-in survey
- **Complexity**: Simple (once trigger is set up)

### Trial Conversion
- **Trigger**: Form submission — contact submits a trial sign-up form; if trial enrollment is programmatic rather than form-based, requires a Custom activity instead _(developer setup)_
- **Value**: Guides trial users to activation milestones and conversion
- **Steps**: Send trial activation email → Wait 3 days → Condition: activated? → True: send advanced tips → False: send help offer → Wait until trial expiry minus 3 days → Send upgrade prompt
- **Complexity**: Moderate

## Content and Subscription

### Newsletter Subscriber Onboarding
- **Trigger**: Form submission — contact submits a newsletter subscription form
- **Value**: Establishes brand familiarity and trust before promotional emails
- **Steps**: Send welcome with best content roundup → Wait 1 week → Send most popular articles → Wait 2 weeks → Invite to webinar or event
- **Complexity**: Simple

### Content Stage Progression
- **Trigger**: Form submission — contact submits a gated content download form
- **Value**: Progressively moves contacts from awareness to consideration content
- **Steps**: Set contact stage attribute → Wait 5 days → Send consideration-stage resource → Wait 5 days → Send decision-stage case study → Condition: contact in target segment? → True: notify sales rep
- **Complexity**: Moderate

## Internal Operations

### High-Intent Sales Notification _(requires developer)_
- **Trigger**: Custom activity — logged when a contact visits a high-intent page (e.g., `pricing_page_visited`) or performs a qualifying download
- **Developer setup**: Activity-logging Page Builder widget placed on the pricing page, or server-side code that logs the activity on page render
- **Value**: Ensures sales team follows up on warm leads quickly
- **Steps**: Condition: contact score or attribute above threshold? → True: notify sales rep and set contact stage → False: continue standard nurture
- **Complexity**: Moderate

### Contact Data Quality _(requires developer)_
- **Trigger**: Custom activity — logged by a scheduled job for contacts with missing key attributes (e.g., `data_quality_review_needed`)
- **Developer setup**: Scheduled background job queries contacts with empty required fields and logs a custom activity for each
- **Value**: Keeps the contact database clean and segmentation accurate
- **Steps**: Send data-update request email → Wait 7 days → Condition: field now populated? → True: set status attribute → False: set flag for manual review
- **Complexity**: Simple (once trigger is set up)

---

## Common Patterns

Across all use cases, the most reused elements are:

- **Triggers**: Form submission (most common, no developer needed), Registration (member sign-up), Custom activity (developer setup required for non-form events such as purchases, page visits, external integrations, and scheduled evaluations)
- **Wait steps**: Fixed duration (days/weeks)
- **Condition branching**: Email open/click, contact field value check, activity logged, recipient list membership
- **Actions**: Send email, set contact field value, notify a team member, log custom activity (to chain into a downstream automation)
- **Outcomes**: Higher conversion rates, faster follow-up, reduced manual effort, cleaner contact data
