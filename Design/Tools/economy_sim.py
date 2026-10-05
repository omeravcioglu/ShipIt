#!/usr/bin/env python3
"""Early-game economy simulator for the IndieStudio GDD (solo founder, first 3 years).

Why this exists: the design promises that the opening years are tight. Going all-in on
your own game should be fast but financially reckless, pure freelancing should be safe
but slow, and balanced strategies should survive on thin margins. This script checks
that the tuning numbers in 13_Finance_and_Economy.md actually produce that shape.

It deliberately models only the solo phase (no hires, publishers, grants, or loans).
Negative cash is allowed so we can see *when* a strategy would force the player into
the emergency options described in 16_Progression_Goals_Failure.md.

Model summary (see CONFIG for every number):
  * Time is spent in 4-hour sessions that the player plays as minigames (Design/02 section 4). The model
    works in weekly hours; a session's minigame score becomes a work multiplier (session_mult: 1.0 is an
    ordinary session, careful play is about 1.1-1.2), applied to gig earnings and game progress.
  * Projects are sized for played sessions: Tiny 1.2 pw (~15 sessions), Small 30 pw (~300) (Design/05).
  * A day job takes energy as well as hours: sessions after a shift count for less (after_job_output).
  * Two founder skills: MAIN (the discipline you freelance in, e.g. programming) and
    BREADTH (art/design/audio: only grows from working on your own games).
    Freelance pay tracks MAIN; game quality tracks both. Outsourcing buys breadth,
    but every outsourced person-week costs you briefing/review hours.
  * Skill XP: hours from level L to L+1 = 70 * L^2.
  * Freelance gigs are scarce until you earn a rating; unfilled hours go to your game.
  * Burnout rises above 45 h/week; the player takes a vacation week at burnout 60.
  * A store page opens partway through development; launch waits for a minimum
    wishlist-building runway, and waiting time is spent polishing.
  * Sales: wishlists x conversion, a 12-month decay plus a small tail, paid ~30 days
    after each month closes. Living costs rise 4% a year.

Usage:
    python economy_sim.py                  # 2,000 runs per strategy
    python economy_sim.py --runs 5000 --seed 7

Standard library only. Every tunable lives in CONFIG and STRATEGIES.
"""
import argparse
import math
import random
import statistics
from collections import defaultdict

CONFIG = {
    "start_cash": 5000,
    "living_cost_month": 1460,       # rent 850, food 450, utilities 90, internet 40, phone 30
    "living_cost_growth_year": 0.04, # rent rises every year
    "tools_month": 35,               # a paid art/audio tool or two
    "weeks": 156,                    # 3 in-game years
    "tax_rate": 0.25,                # simplified self-employment tax
    "tax_allowance_year": 10000,     # untaxed income per year

    # Founder skills (1-10). Hours from level L to L+1 = coeff * L^2
    "start_main": 2.5,
    "start_breadth": 1.5,
    "xp_hours_coeff": 70,
    "dev_main_share": 0.4,           # share of own-game hours that train MAIN (rest -> BREADTH)
    "outsourced_skill_range": (3.5, 6.5),  # vendor quality is a gamble until you know good contractors

    # Freelance (GigGrid): $/h = (base + mult * main) * (1 + rating bonus)
    "rate_base": 5, "rate_mult": 4.5,
    "rating_bonus_max": 0.25,
    "rating_hours_scale": 400,       # rating = 5 * (1 - e^(-hours/scale))
    "fill_start": 0.60,              # share of wanted gig hours actually available
    "fill_max": 0.95,
    "nonpay_risk_start": 0.06,       # weekly chance a client ghosts you
    "nonpay_risk_min": 0.01,

    # Day jobs (fixed hours, no skill growth)
    "jobs": {"part": dict(hours=20, month=1600), "full": dict(hours=40, month=2900)},
    "after_job_output": {"part": 0.9, "full": 0.8},   # tired evenings: Tuning.AfterJobFatigue (part-time: half the sessions)

    # Development: 1 person-week (pw) = 40h at skill-5 efficiency
    "pw_hours": 40,
    "eff_base": 0.55, "eff_per_skill": 0.09,          # efficiency = 0.55 + 0.09 * weighted skill
    "scope_pw": {"tiny": 1.2, "small": 30},
    "asset_spend": {"tiny": 80, "small": 1200},
    "asset_work_saving": {"tiny": 0.10, "small": 0.15},
    "outsource_cost_per_pw": 1300,
    "outsource_mgmt_hours_per_pw": 5, # briefing + review time taken from your own dev hours
    "outsource_reserve_months": 3,    # never outsource below this runway

    # Minigames: a session's score -> work multiplier 0.5-1.2 (Rules.MinigameMultiplier). 1.0 = ordinary.
    "session_mult": 1.0,

    # One-off reinvestments a sensible player makes
    "hardware_week": 26, "hardware_cost": 1800, "hardware_min_cash": 4000,
    "store_fee": 100,                # Vapor Direct fee per game
    "capsule_art": {"tiny": 0, "small": 1200},
    "launch_ads": {"tiny": 0, "small": 500},

    # Store page / launch runway
    "store_page_at_progress": {"tiny": 0.3, "small": 0.3},
    "min_store_weeks": {"tiny": 2, "small": 21},     # Vapor's 14-day minimum; ~5 months of wishlists for a Small game
    "pre_page_marketing_value": 0.5,
    "polish_q_per_pw": 0.01, "polish_q_cap": 0.08,

    # Burnout
    "burnout_threshold_h": 45,
    "burnout_up_per_h": 0.35, "burnout_down_per_h": 0.5,
    "vacation_at_burnout": 60,
    "collapse_burnout": 75, "collapse_chance": 0.08, "collapse_weeks": 2,

    # Quality: q = base + per_skill * weighted skill + bonuses + noise
    "q_base": 0.30, "q_per_skill": 0.075, "q_noise": 0.12,
    "tiny_ship_q_bonus": 0.05,       # first-launch lessons carried into the next game
    "tiny_ship_breadth_bonus": 0.3,
    "tiny_audience_carryover": 0.4,  # share of tiny wishlists+buyers who wishlist the next game

    # Marketing -> wishlists
    "wl_per_mkt_hour_median": 6.0, "wl_per_mkt_hour_sigma": 0.8,
    "nextfest_median": 1500, "nextfest_sigma": 0.9,
    "tiny_wishlists_median": 350,

    # Sales
    "price": {"tiny": 3.99, "small": 12.99},
    "net_per_gross": 0.90 * 0.92 * 0.70,   # avg 10% discount, 8% refunds, 30% platform fee
    "wcr_base": 0.10, "wcr_sigma": 0.35,   # wishlist -> first-month purchase
    "organic_share": 0.30,                 # extra month-1 buyers from store visibility, x quality
    "year1_mult_base": 1.8, "year1_mult_q": 2.2,
    "tail_share_per_month": 0.03,          # months 13-24, share of year-1 units each
    "viral_quality_gate": 0.6, "viral_chance": 0.03, "viral_mult_median": 8,
    "payout_lag_weeks": 6,                 # platform pays ~30 days after month end
    "hit_threshold": 100_000,              # year-1 net that counts as a breakout
}

TINY_THEN_SMALL = ("tiny", "small")
STRATEGIES = [
    dict(name="All-in on own game",        freelance=0,  dev=40, marketing=6, job=None,   outsource=False, plan=TINY_THEN_SMALL),
    dict(name="Balanced",                  freelance=20, dev=22, marketing=6, job=None,   outsource=False, plan=TINY_THEN_SMALL),
    dict(name="Balanced, careful play",    freelance=20, dev=22, marketing=6, job=None,   outsource=False, plan=TINY_THEN_SMALL, session_mult=1.12),
    dict(name="Balanced, skip Tiny game",  freelance=20, dev=22, marketing=6, job=None,   outsource=False, plan=("small",)),
    dict(name="Balanced + crunch (60h)",   freelance=22, dev=30, marketing=8, job=None,   outsource=False, plan=TINY_THEN_SMALL),
    dict(name="Freelance-heavy",           freelance=32, dev=12, marketing=4, job=None,   outsource=False, plan=TINY_THEN_SMALL),
    dict(name="Freelance + outsource",     freelance=28, dev=14, marketing=6, job=None,   outsource=True,  plan=TINY_THEN_SMALL),
    dict(name="Part-time job",             freelance=0,  dev=22, marketing=6, job="part", outsource=False, plan=TINY_THEN_SMALL),
    dict(name="Full-time job + evenings",  freelance=0,  dev=12, marketing=3, job="full", outsource=False, plan=TINY_THEN_SMALL),
]


def lognormal(rng, median, sigma):
    return median * math.exp(rng.gauss(0.0, sigma))


def clamp(x, lo, hi):
    return max(lo, min(hi, x))


def weekly_hours(strategy):
    job_hours = CONFIG["jobs"][strategy["job"]]["hours"] if strategy["job"] else 0
    return strategy["freelance"] + strategy["dev"] + strategy["marketing"] + job_hours


def hourly_rate(main, rating):
    c = CONFIG
    return (c["rate_base"] + c["rate_mult"] * main) * (1 + c["rating_bonus_max"] * rating / 5)


def gain_xp(skill, hours):
    """Advance a 1-10 skill by `hours` of practice (level L -> L+1 costs coeff * L^2 hours)."""
    while hours > 0 and skill < 10:
        level = max(1, math.floor(skill))
        cost_left = CONFIG["xp_hours_coeff"] * level ** 2 * (level + 1 - skill)
        if hours >= cost_left:
            hours -= cost_left
            skill = level + 1
        else:
            skill += hours / (CONFIG["xp_hours_coeff"] * level ** 2)
            hours = 0
    return min(skill, 10.0)


class Project:
    def __init__(self, scope):
        c = CONFIG
        self.scope = scope
        self.work_needed = c["scope_pw"][scope] * (1 - c["asset_work_saving"][scope])
        self.founder_pw = 0.0
        self.outsourced_pw = 0.0
        self.polish_pw = 0.0
        self.marketing_value = 0.0
        self.store_page_week = None
        self.vendor_skill = None

    @property
    def work_done(self):
        return self.founder_pw + self.outsourced_pw

    @property
    def dev_complete(self):
        return self.work_done >= self.work_needed


def launch(rng, project, main, breadth, q_bonus, carryover_wishlists):
    """Resolve a release. Returns quality, wishlists and unit sales."""
    c = CONFIG
    total = max(project.work_done, 1e-9)
    vendor = project.vendor_skill or 0.0
    eff_breadth = (breadth * project.founder_pw + vendor * project.outsourced_pw) / total
    skill = 0.4 * main + 0.6 * eff_breadth
    polish = min(c["polish_q_cap"], project.polish_pw * c["polish_q_per_pw"])
    q = clamp(c["q_base"] + c["q_per_skill"] * skill + q_bonus + polish + rng.gauss(0, c["q_noise"]), 0.05, 0.98)
    page = 0.6 + 0.8 * q  # store page / hook quality tracks game quality
    if project.scope == "tiny":
        wishlists = lognormal(rng, c["tiny_wishlists_median"], 0.7) * page
    else:
        per_hour = lognormal(rng, c["wl_per_mkt_hour_median"], c["wl_per_mkt_hour_sigma"])
        wishlists = project.marketing_value * per_hour * page
        wishlists += lognormal(rng, c["nextfest_median"], c["nextfest_sigma"]) * page
    wishlists += carryover_wishlists
    wcr = c["wcr_base"] * (0.5 + q) * math.exp(rng.gauss(0, c["wcr_sigma"]))
    month1 = wishlists * wcr
    month1 += c["organic_share"] * month1 * q
    year1 = month1 * (c["year1_mult_base"] + c["year1_mult_q"] * q)
    viral = q > c["viral_quality_gate"] and rng.random() < c["viral_chance"]
    if viral:
        boost = lognormal(rng, c["viral_mult_median"], 0.6)
        month1 *= boost
        year1 *= boost
    return dict(q=q, wishlists=wishlists, month1=month1, year1=year1, viral=viral)


def schedule_payouts(payouts, launch_week, scope, result):
    """Spread 24 months of unit sales and pay each month ~30 days after it closes."""
    c = CONFIG
    net_unit = c["price"][scope] * c["net_per_gross"]
    rest = result["year1"] - result["month1"]
    weights = [0.78 ** i for i in range(11)]
    total_w = sum(weights)
    monthly = [result["month1"]] + [rest * w / total_w for w in weights]
    monthly += [result["year1"] * c["tail_share_per_month"]] * 12
    for m, units in enumerate(monthly):
        payouts[launch_week + 4 * m + c["payout_lag_weeks"]] += units * net_unit
    return result["year1"] * net_unit


def run_once(strategy, rng):
    c = CONFIG
    cash = float(c["start_cash"])
    main, breadth = c["start_main"], c["start_breadth"]
    burnout = 0.0
    sick_weeks = collapses = vacations = 0
    fl_hours_total = 0.0
    payouts = defaultdict(float)
    year_income = year_expenses = 0.0
    bought_hardware = False
    q_bonus = 0.0
    carryover = 0.0
    job = c["jobs"][strategy["job"]] if strategy["job"] else None
    session_mult = strategy.get("session_mult", c["session_mult"])

    plan = list(strategy["plan"])
    project = None

    def spend(amount):
        nonlocal cash, year_expenses
        cash -= amount
        year_expenses += amount

    def start_next():
        nonlocal project
        project = Project(plan.pop(0)) if plan else None
        if project:
            spend(c["asset_spend"][project.scope])

    start_next()
    out = dict(first_negative_month=None, min_cash=cash, launches={}, cash_at={})

    for week in range(c["weeks"]):
        year = week // 52
        living = c["living_cost_month"] * (1 + c["living_cost_growth_year"]) ** year
        cash -= (living + c["tools_month"]) * 12 / 52

        paid = payouts.pop(week, 0.0)
        cash += paid
        year_income += paid

        if job:
            job_pay = job["month"] * 12 / 52
            cash += job_pay
            year_income += job_pay

        if not bought_hardware and week >= c["hardware_week"] and cash >= c["hardware_min_cash"]:
            spend(c["hardware_cost"])
            bought_hardware = True

        working = True
        if sick_weeks > 0:
            sick_weeks -= 1
            working = False
        elif burnout >= c["vacation_at_burnout"]:
            vacations += 1
            working = False

        if not working:
            burnout = max(0.0, burnout - c["burnout_down_per_h"] * c["burnout_threshold_h"])
        else:
            hours = weekly_hours(strategy)
            if hours > c["burnout_threshold_h"]:
                burnout += c["burnout_up_per_h"] * (hours - c["burnout_threshold_h"])
            else:
                burnout -= c["burnout_down_per_h"] * (c["burnout_threshold_h"] - hours)
            burnout = clamp(burnout, 0.0, 100.0)
            eff_mult = 1 - 0.004 * max(0.0, burnout - 30)
            if burnout > c["collapse_burnout"] and rng.random() < c["collapse_chance"]:
                sick_weeks = c["collapse_weeks"]
                collapses += 1

            # Freelance: gigs are scarce until you have a rating; unfilled hours go to your game.
            rating = 5 * (1 - math.exp(-fl_hours_total / c["rating_hours_scale"]))
            fill = c["fill_start"] + (c["fill_max"] - c["fill_start"]) * rating / 5
            fl_hours = strategy["freelance"] * fill
            spill = strategy["freelance"] - fl_hours
            nonpay = c["nonpay_risk_start"] + (c["nonpay_risk_min"] - c["nonpay_risk_start"]) * rating / 5
            if fl_hours > 0 and rng.random() >= nonpay:
                earned = fl_hours * hourly_rate(main, rating) * session_mult  # careful sessions finish gigs sooner
                cash += earned
                year_income += earned
            fl_hours_total += fl_hours

            dev_hours = (strategy["dev"] + spill) * eff_mult
            if job:
                dev_hours *= c["after_job_output"][strategy["job"]]

            # Outsourcing converts cash into work, but you pay for it in briefing/review hours.
            if project and strategy["outsource"] and not project.dev_complete:
                reserve = c["outsource_reserve_months"] * living
                price = c["outsource_cost_per_pw"]
                mgmt = c["outsource_mgmt_hours_per_pw"]
                if cash > reserve + price:
                    buy = min(1.0, (cash - reserve) / price,
                              project.work_needed - project.work_done, dev_hours / mgmt)
                    if buy > 0:
                        if project.vendor_skill is None:
                            project.vendor_skill = rng.uniform(*c["outsourced_skill_range"])
                        spend(buy * price)
                        project.outsourced_pw += buy
                        dev_hours -= buy * mgmt

            main = gain_xp(main, fl_hours * eff_mult + dev_hours * c["dev_main_share"])
            breadth = gain_xp(breadth, dev_hours * (1 - c["dev_main_share"]))

            if project:
                efficiency = c["eff_base"] + c["eff_per_skill"] * (0.4 * main + 0.6 * breadth)
                pw = dev_hours * efficiency / c["pw_hours"] * session_mult
                if project.dev_complete:
                    project.polish_pw += pw
                else:
                    project.founder_pw += pw
                live = project.store_page_week is not None
                project.marketing_value += strategy["marketing"] * (1.0 if live else c["pre_page_marketing_value"])

        if project:
            if project.store_page_week is None and project.work_done >= c["store_page_at_progress"][project.scope] * project.work_needed:
                project.store_page_week = week
                spend(c["store_fee"] + c["capsule_art"][project.scope])

            runway_ok = (project.store_page_week is not None
                         and week - project.store_page_week >= c["min_store_weeks"][project.scope])
            if project.dev_complete and runway_ok:
                spend(c["launch_ads"][project.scope])
                result = launch(rng, project, main, breadth, q_bonus, carryover)
                result["year1_net"] = schedule_payouts(payouts, week, project.scope, result)
                result["month"] = week / (52 / 12)
                out["launches"][project.scope] = result
                if project.scope == "tiny":
                    q_bonus += c["tiny_ship_q_bonus"]
                    breadth = min(10.0, breadth + c["tiny_ship_breadth_bonus"])
                    carryover = c["tiny_audience_carryover"] * (result["wishlists"] + result["year1"])
                start_next()

        if (week + 1) % 52 == 0:
            taxable = year_income - year_expenses - c["tax_allowance_year"]
            if taxable > 0:
                cash -= taxable * c["tax_rate"]
            year_income = year_expenses = 0.0

        out["min_cash"] = min(out["min_cash"], cash)
        if cash < 0 and out["first_negative_month"] is None:
            out["first_negative_month"] = int(week / (52 / 12)) + 1
        if (week + 1) in (52, 104, 156):
            out["cash_at"][(week + 1) // 52 * 12] = cash

    out.update(main=main, breadth=breadth, collapses=collapses, vacations=vacations)
    return out


def pct(values, p):
    if not values:
        return float("nan")
    values = sorted(values)
    k = (len(values) - 1) * p
    lo, hi = math.floor(k), math.ceil(k)
    return values[lo] + (values[hi] - values[lo]) * (k - lo)


def money(x):
    if x != x:  # NaN
        return "-"
    sign = "-" if x < 0 else ""
    x = abs(x)
    return f"{sign}${x / 1000:.1f}k" if x >= 1000 else f"{sign}${x:.0f}"


def summarize(strategy, runs):
    broke = [r for r in runs if r["first_negative_month"] is not None]
    smalls = [r["launches"]["small"] for r in runs if "small" in r["launches"]]
    tinies = [r["launches"]["tiny"] for r in runs if "tiny" in r["launches"]]
    hits = [s for s in smalls if s["year1_net"] >= CONFIG["hit_threshold"]]
    nets = [s["year1_net"] for s in smalls]
    return {
        "Strategy": strategy["name"],
        "h/wk": weekly_hours(strategy),
        "Cash < 0": f"{100 * len(broke) / len(runs):.1f}%",
        "Broke at (med. month)": f"{statistics.median(r['first_negative_month'] for r in broke):.0f}" if len(broke) >= 20 else "-",
        "Lowest cash (med.)": money(statistics.median(r["min_cash"] for r in runs)),
        "Tiny ships (med. month)": f"{statistics.median(t['month'] for t in tinies):.0f}" if tinies else "-",
        "Small game ships (med. month)": f"{statistics.median(s['month'] for s in smalls):.0f}" if smalls else "-",
        "Shipped by M36": f"{100 * len(smalls) / len(runs):.0f}%",
        "Launch wishlists (med.)": f"{statistics.median(s['wishlists'] for s in smalls):,.0f}" if smalls else "-",
        "Small game yr-1 net P10 / P50 / P90": " / ".join(money(pct(nets, p)) for p in (0.1, 0.5, 0.9)) if smalls else "-",
        "Hit (>=$100k)": f"{100 * len(hits) / len(runs):.1f}%",
        "Cash M24 (med.)": money(statistics.median(r["cash_at"][24] for r in runs)),
        "Cash M36 (med.)": money(statistics.median(r["cash_at"][36] for r in runs)),
        "Main / breadth skill M36": f"{statistics.median(r['main'] for r in runs):.1f} / {statistics.median(r['breadth'] for r in runs):.1f}",
        "Vacations / collapses": f"{statistics.mean(r['vacations'] for r in runs):.1f} / {statistics.mean(r['collapses'] for r in runs):.1f}",
    }


def print_markdown(rows):
    headers = list(rows[0].keys())
    print("| " + " | ".join(headers) + " |")
    print("|" + "|".join("---" for _ in headers) + "|")
    for row in rows:
        print("| " + " | ".join(str(row[h]) for h in headers) + " |")


def main():
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("--runs", type=int, default=2000)
    parser.add_argument("--seed", type=int, default=1)
    args = parser.parse_args()

    rows = []
    for i, strategy in enumerate(STRATEGIES):
        rng = random.Random(args.seed * 1000 + i)
        runs = [run_once(strategy, rng) for _ in range(args.runs)]
        rows.append(summarize(strategy, runs))
    print_markdown(rows)


if __name__ == "__main__":
    main()
