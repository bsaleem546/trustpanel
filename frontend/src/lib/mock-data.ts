export type TestimonialStatus = "pending" | "approved" | "featured" | "rejected";
export type Sentiment = "positive" | "neutral" | "negative";

export interface Testimonial {
  id: string;
  name: string;
  jobTitle: string;
  company: string;
  avatarColor: string;
  rating: number;
  text: string;
  type: "text" | "video";
  videoDuration?: string;
  source: "Form" | "Google" | "Twitter" | "G2" | "Manual";
  status: TestimonialStatus;
  sentiment: Sentiment;
  tags: string[];
  date: string;
  country?: string;
}

const palette = ["#7c6af7", "#34d399", "#60a5fa", "#fbbf24", "#f87171", "#a594f9"];
const color = (i: number) => palette[i % palette.length];

export const testimonials: Testimonial[] = [
  {
    id: "t1",
    name: "Sarah Kowalski",
    jobTitle: "Head of Growth",
    company: "Northwind Studios",
    avatarColor: color(0),
    rating: 5,
    text: "TrustPanel cut our review collection workflow from days to minutes. The white-label setup means we ship it under our own brand to every client — they think we built it.",
    type: "text",
    source: "Form",
    status: "pending",
    sentiment: "positive",
    tags: ["agency", "white-label"],
    date: "2h ago",
    country: "US",
  },
  {
    id: "t2",
    name: "Marco Bertelli",
    jobTitle: "Founder",
    company: "Pixelhaus",
    avatarColor: color(1),
    rating: 5,
    text: "The in-browser video recording is the killer feature. We doubled our testimonial collection rate the first month.",
    type: "video",
    videoDuration: "0:47",
    source: "Form",
    status: "pending",
    sentiment: "positive",
    tags: ["video", "conversion"],
    date: "5h ago",
    country: "IT",
  },
  {
    id: "t3",
    name: "Priya Raman",
    jobTitle: "Marketing Director",
    company: "Loomwise",
    avatarColor: color(2),
    rating: 4,
    text: "Embedding widgets across three different CMS platforms was painless thanks to the shadow DOM approach — zero CSS conflicts.",
    type: "text",
    source: "Google",
    status: "approved",
    sentiment: "positive",
    tags: ["widgets", "integration"],
    date: "1d ago",
    country: "IN",
  },
  {
    id: "t4",
    name: "Jonas Lindqvist",
    jobTitle: "CTO",
    company: "Fjordlab",
    avatarColor: color(3),
    rating: 5,
    text: "The AI sentiment scoring actually catches the lukewarm reviews so we can address them before they go public. Game-changer.",
    type: "text",
    source: "Form",
    status: "featured",
    sentiment: "positive",
    tags: ["ai", "insights"],
    date: "2d ago",
    country: "SE",
  },
  {
    id: "t5",
    name: "Aisha Bennett",
    jobTitle: "Content Lead",
    company: "Roamletter",
    avatarColor: color(4),
    rating: 3,
    text: "Solid product. The mobile widget could use more layout options but overall does what it promises.",
    type: "text",
    source: "Twitter",
    status: "approved",
    sentiment: "neutral",
    tags: ["feedback"],
    date: "3d ago",
    country: "GB",
  },
  {
    id: "t6",
    name: "Daniel Okafor",
    jobTitle: "Product Manager",
    company: "Mintledger",
    avatarColor: color(5),
    rating: 5,
    text: "Setup took 11 minutes. Eleven. We had testimonials embedded on our pricing page that same afternoon.",
    type: "video",
    videoDuration: "1:12",
    source: "Form",
    status: "approved",
    sentiment: "positive",
    tags: ["speed", "onboarding"],
    date: "4d ago",
    country: "NG",
  },
  {
    id: "t7",
    name: "Camille Boucher",
    jobTitle: "Designer",
    company: "Studio Vermillion",
    avatarColor: color(0),
    rating: 5,
    text: "The widget aesthetics finally don't look like a third-party plugin. We can match our exact brand system.",
    type: "text",
    source: "Form",
    status: "featured",
    sentiment: "positive",
    tags: ["design", "branding"],
    date: "5d ago",
    country: "FR",
  },
  {
    id: "t8",
    name: "Hugo Rivera",
    jobTitle: "Ops Lead",
    company: "Cargobird",
    avatarColor: color(1),
    rating: 2,
    text: "Documentation around the API is a bit thin in places. The product itself works fine though.",
    type: "text",
    source: "G2",
    status: "rejected",
    sentiment: "negative",
    tags: ["api", "docs"],
    date: "1w ago",
    country: "ES",
  },
];

export const forms = [
  { id: "f1", name: "Homepage testimonial", submissions: 47, conversion: 32, types: ["text", "video"], active: true, created: "Jan 12, 2025" },
  { id: "f2", name: "Post-purchase NPS", submissions: 128, conversion: 41, types: ["text"], active: true, created: "Feb 04, 2025" },
  { id: "f3", name: "Webinar attendee follow-up", submissions: 19, conversion: 17, types: ["video"], active: false, created: "Mar 21, 2025" },
  { id: "f4", name: "Enterprise customer case study", submissions: 8, conversion: 67, types: ["text", "video"], active: true, created: "Apr 02, 2025" },
];

export const activity = [
  { who: "Sarah K.", what: "submitted a video testimonial", when: "2 hours ago", color: "var(--primary)" },
  { who: "You", what: "approved 3 testimonials", when: "5 hours ago", color: "var(--success)" },
  { who: "Widget 'Homepage Carousel'", what: "got 500 impressions", when: "yesterday", color: "var(--info)" },
  { who: "Marco B.", what: "submitted a video testimonial", when: "yesterday", color: "var(--primary)" },
  { who: "Sync", what: "imported 6 new Google reviews", when: "2 days ago", color: "var(--warning)" },
  { who: "You", what: "published widget 'Pricing Wall'", when: "3 days ago", color: "var(--success)" },
];

export const impressionsData = Array.from({ length: 14 }).map((_, i) => ({
  day: `Day ${i + 1}`,
  impressions: Math.round(600 + Math.sin(i / 2) * 200 + i * 35),
}));

export const submissionsByForm = forms.map((f) => ({ name: f.name.split(" ")[0], submissions: f.submissions }));

export const sentimentTrend = Array.from({ length: 12 }).map((_, i) => ({
  month: `M${i + 1}`,
  score: +(0.3 + Math.sin(i / 2) * 0.25 + i * 0.02).toFixed(2),
}));

export const ratingDistribution = [
  { stars: "5★", pct: 72 },
  { stars: "4★", pct: 18 },
  { stars: "3★", pct: 6 },
  { stars: "2★", pct: 3 },
  { stars: "1★", pct: 1 },
];
