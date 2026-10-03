import { describe, expect, it } from 'vitest';
import { csvText, filterDates, revenue, type Row } from './domain';

describe('report revenue ledger', () => {
  it('separates wallet funding and subtracts refunds without summing unrelated records', () => {
    const rows: Row[] = [
      { id: 'rental', type: 'Rental', amount: 20000 }, { id: 'food', type: 'FoodOrder', amount: 35000 },
      { id: 'refund', type: 'Refund', amount: 10000 }, { id: 'topup', type: 'TopUp', amount: 200000 },
      { id: 'other', type: 'Pending', amount: 900000 },
    ];
    expect(revenue(rows)).toEqual({ rental: 20000, food: 35000, refund: 10000, net: 45000, topup: 200000 });
    expect(revenue(rows.filter(row => row.type !== 'TopUp')).net).toBe(revenue(rows).net);
  });
  it('allows a refund-only reporting period to have negative net revenue', () => {
    expect(revenue([{ id: 'refund', type: 'Refund', amount: 50000 }])).toEqual({ rental: 0, food: 0, refund: 50000, net: -50000, topup: 0 });
    expect(revenue([])).toEqual({ rental: 0, food: 0, refund: 0, net: 0, topup: 0 });
  });
  it('does not modify source transactions while aggregating', () => {
    const row = Object.freeze({ id: 'tx', type: 'Rental', amount: 15000 });
    expect(revenue([row]).net).toBe(15000); expect(row.amount).toBe(15000);
  });
});

describe('local calendar date filter', () => {
  const instant = (day: number, hour: number, minute = 0) => new Date(2026, 9, day, hour, minute).toISOString();
  const rows: Row[] = [
    { id: 'before', createdAt: instant(2, 23, 59) }, { id: 'start', createdAt: instant(3, 0) },
    { id: 'end', createdAt: instant(3, 23, 59) }, { id: 'after', createdAt: instant(4, 0) },
    { id: 'missing' }, { id: 'invalid', createdAt: 'not-a-date' },
  ];
  it('includes both boundaries of the selected browser-local calendar day', () => {
    expect(filterDates(rows, '2026-10-03', '2026-10-03').map(row => row.id)).toEqual(['start', 'end']);
  });
  it('handles open bounds, reversed ranges and missing timestamps explicitly', () => {
    expect(filterDates(rows, '', '2026-10-03').map(row => row.id)).toEqual(['before', 'start', 'end']);
    expect(filterDates(rows, '2026-10-03', '').map(row => row.id)).toEqual(['start', 'end', 'after']);
    expect(filterDates(rows, '2026-10-04', '2026-10-03')).toEqual([]);
    expect(filterDates(rows, '', '')).toEqual(rows);
  });
  it('filters a specified source field without using an unrelated timestamp', () => {
    expect(filterDates([{ id: 'leave', createdAt: instant(1, 12), startDate: instant(3, 8) }], '2026-10-03', '2026-10-03', 'startDate')).toHaveLength(1);
  });
});

describe('CSV spreadsheet compatibility and formula escaping', () => {
  const columns = [{ key: 'value', label: 'Giá trị' }];
  it.each(['=SUM(A1:A2)', '+1+1', '-1+1', '@SUM(A1:A2)', '\t=1+1', '\r=1+1'])('escapes a formula-like cell %j', value => {
    expect(csvText([{ id: 'cell', value }], columns)).toBe(`\uFEFF"Giá trị"\r\n"'${value}"`);
  });
  it('retains Unicode, commas, multiline text and quotes in quoted cells', () => {
    expect(csvText([{ id: 'cell', value: 'Khách "Huy", dòng 1\ndòng 2' }], columns)).toBe('\uFEFF"Giá trị"\r\n"Khách ""Huy"", dòng 1\ndòng 2"');
  });
  it('applies the same escaping to column labels and handles missing values', () => {
    expect(csvText([{ id: 'row', value: null }], [{ key: 'value', label: '=Header' }])).toBe('\uFEFF"\'=Header"\r\n""');
  });
  it('exports only requested columns in requested order without mutation', () => {
    const row = Object.freeze({ id: 'r', name: 'Linh', amount: 25000, hidden: 'not exported' });
    expect(csvText([row], [{ key: 'amount', label: 'Số tiền' }, { key: 'name', label: 'Khách' }])).toBe('\uFEFF"Số tiền","Khách"\r\n"25000","Linh"');
  });
});
