import json
import os
import re

# 300 words pulled from the corporate document

WORDS = [
"corporate", "strategy", "infrastructure", "audit", "report",
"financial", "outlook", "revenue", "portfolio", "saturation",
"acquisition", "lifetime", "emerging", "economies", "trading",
"algorithmic", "margins", "exceeded", "regulatory", "currency",
"planning", "quarters", "bullish", "innovation", "framework",
"sustainable", "reduction", "capital", "expansion", "technology",
"serverless", "architecture", "scalability", "demand", "hardware",
"liquid", "cooling", "overclocking", "availability", "satisfaction",
"migration", "distributed", "ledger", "compliance", "encryption",
"security", "artificial", "intelligence", "operational", "predictive",
"optimization", "energy", "waste", "balancing", "historical",
"patterns", "industrial", "reroute", "preventing", "brownouts",
"machine", "learning", "customer", "behavior", "production",
"maturity", "churn", "predicting", "engagement", "personalized",
"retention", "transparent", "auditable", "overseers", "ethics",
"committee", "explainable", "automated", "decision", "scoring",
"partnerships", "volatile", "markets", "strategic", "consumer",
"sophisticated", "management", "contract", "predictable", "supply",
"insulating", "competitors", "natural", "renewable", "storage",
"development", "patent", "applications", "duration", "analytics",
"solar", "licensing", "opportunities", "workforce", "remote",
"hybrid", "schedules", "happiness", "metrics", "absenteeism",
"training", "developers", "architects", "productivity", "proprietary",
"pipeline", "shipping", "logistics", "bottlenecks", "automation",
"warehouse", "manual", "processing", "deployment", "collaborative",
"inventory", "digital", "simulate", "scenarios", "enterprise",
"healthcare", "education", "marketing", "campaigns", "targeted",
"advertising", "conversion", "channels", "acquisition", "legal",
"merger", "documents", "integration", "multinational", "regulatory",
"stakeholder", "confidence", "performance", "institutional", "excellence",
"champions", "trophyless", "drought", "complacency", "dominant",
"stagnation", "paradigm", "galacticos", "foundational", "resilience",
"silverware", "reinvestment", "superstar", "parallel", "pivot",
"healthcare", "pharmaceutical", "genomic", "sequencing", "discovery",
"stringent", "differentiator", "lifelong", "platforms", "automated",
"grading", "positioning", "infrastructure", "projected", "foothold",
"conclusion", "roadmap", "priority", "machine", "learning",
"expansion", "utility", "aggressive", "sovereignty", "vendor",
"hardening", "threats", "disruptions", "trajectory", "aligned",
"geopolitical", "alliances", "diversified", "stabilizer", "preliminary",
"theater", "definitive", "narrowing", "delta", "mandated",
"burgeoning", "hubs", "centralized", "proprietary", "leverage",
"moderate", "decline", "failure", "fluctuating", "valuations",
"eroded", "purchasing", "contingent", "adherence", "allocations",
"division", "transformative", "maturation", "decoupled", "infinite",
"overhead", "idle", "investments", "implemented", "documented",
"downtime", "measurable", "increase", "successfully", "completed",
"critical", "simultaneously", "rigorous", "third-party", "findings",
"landscape", "shifting", "residency", "protocols", "jurisdictions",
"required", "immediate", "updates", "standards", "default",
"posture", "experimental", "sandbox", "distribution", "reduce",
"analyzing", "weather", "activity", "cycles", "models",
"mechanical", "strain", "official", "reached", "reacting",
"advance", "utilize", "deep", "subtle", "shifts",
"intervene", "cognizant", "black", "box", "framework",
"ensuring", "credit", "resource", "allocation", "remains",
"renewal", "long-term", "secures", "stable", "crippled",
"committed", "carbon", "neutral", "bridge", "fuel",
"catch", "demand", "filing", "twelve", "focused",
"primarily", "owning", "saving", "creating", "stream",
"realignment", "labor", "reported", "significant", "attribute",
"directly", "designed", "double-digit", "decrease", "burnout",
"program", "junior", "senior", "external", "competing",
"hyper", "expensive", "growing", "internal", "trained",
"specifically", "contributed", "higher", "technical", "departments",
"remained", "peak", "efficiency", "testament", "foresight",
"ongoing", "global", "challenges", "multimodal", "ensured",
"dropped", "safety", "stock", "level", "successful",
"expenditure", "initiatives", "largely", "through", "alongside",
"upgraded", "technology", "simulate", "various", "stress",
"handling", "increased", "breaking", "chain", "surgical",
"spectrum", "awareness", "launched", "highly", "value",
"sovereign", "cloud", "trust", "integrity", "primary",
"purchasing", "drivers", "optimized", "cutting", "doubling",
"achieved", "lower", "previous", "quarter", "finalized",
"upcoming", "complex", "different", "bodies", "successful",
"navigation", "process", "kept", "evidenced", "stable",
"announcement", "phase", "analyzing", "non-corporate", "entities",
"structural", "insights", "objectively", "premier", "frequently",
"cited", "record", "titles", "merely", "sports"
]

def main():
    output_dir = "stress-test-data"
    os.makedirs(output_dir, exist_ok=True)

    # Deduplicate while preserving order
    seen = set()
    unique_words = []
    for w in WORDS:
        if w not in seen:
            seen.add(w)
            unique_words.append(w)

    if len(unique_words) < 300:
        print(f"Warning: only {len(unique_words)} unique words — padding with indexed variants")
        i = 0
        while len(unique_words) < 300:
            unique_words.append(f"search{i}")
            i += 1

    for i in range(300):
        filename = os.path.join(output_dir, f"{i:03d}.json")
        payload = {
            "query": [unique_words[i]],
            "maxAmount": 20,
            "caseSensitive": False
        }
        with open(filename, "w") as f:
            json.dump(payload, f)

    print(f"Generated 300 files in ./{output_dir}/")
    print(f"  First: 000.json -> {unique_words[0]}")
    print(f"  Last:  299.json -> {unique_words[299]}")

if __name__ == "__main__":
    main()